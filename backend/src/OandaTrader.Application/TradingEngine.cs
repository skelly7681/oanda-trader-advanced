using System.Collections.Concurrent;
using OandaTrader.Domain;

namespace OandaTrader.Application;

public sealed class TradingEngine
{
    private readonly IBrokerGateway _broker;
    private readonly IMarketDataClient _marketData;
    private readonly IStrategyRegistry _strategies;
    private readonly IAuditStore _audit;
    private readonly IKillSwitchStore _killSwitch;
    private readonly RiskGate _riskGate;

    private readonly ConcurrentDictionary<string, int> _tradeCountsByInstrument = new();

    public TradingEngine(
        IBrokerGateway broker,
        IMarketDataClient marketData,
        IStrategyRegistry strategies,
        IAuditStore audit,
        IKillSwitchStore killSwitch,
        RiskGate riskGate)
    {
        _broker = broker;
        _marketData = marketData;
        _strategies = strategies;
        _audit = audit;
        _killSwitch = killSwitch;
        _riskGate = riskGate;
    }

    public async Task<StrategyExecutionResult> EvaluateAndMaybeTradeAsync(
        StrategyRunRequest request,
        decimal maxRiskPerTradeFraction,
        decimal maxDailyLossFraction,
        int maxTradesPerDayPerInstrument,
        decimal maxSpread,
        decimal maxLeverage,
        bool stopLossRequired,
        CancellationToken ct)
    {
        var kill = await _killSwitch.GetStateAsync(ct);
        if (kill == KillSwitchState.Engaged)
            throw new InvalidOperationException("Kill switch is engaged.");

        if (request.Mode == TradingMode.Live && !request.AcceptLiveRisk)
            throw new InvalidOperationException("Live mode requires explicit acceptance.");

        var strategy = _strategies.Resolve(request.StrategyName);
        var account = await _broker.GetAccountSnapshotAsync(ct);
        var tick = (await _broker.GetLatestPricesAsync(new[] { request.Instrument }, ct)).Single();

        var candles = await _marketData.GetCandlesAsync(
            request.Instrument,
            request.Granularity,
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow,
            ct);

        var context = new StrategyContext
        {
            Instrument = request.Instrument,
            Granularity = request.Granularity,
            Candles = candles,
            CurrentTick = tick,
            Now = DateTimeOffset.UtcNow
        };

        var rawSignal = strategy.GenerateSignal(context);
        var signal = ResizeSignalToRisk(rawSignal, account, tick, maxRiskPerTradeFraction);

        await _audit.AppendAsync("signal", signal, ct);

        var tradesToday = _tradeCountsByInstrument.GetValueOrDefault(request.Instrument, 0);
        var riskDecision = _riskGate.Evaluate(
            account,
            signal,
            tick,
            tradesToday,
            maxRiskPerTradeFraction,
            maxDailyLossFraction,
            maxTradesPerDayPerInstrument,
            maxSpread,
            maxLeverage,
            stopLossRequired);

        await _audit.AppendAsync("risk-decision", riskDecision, ct);

        if (!riskDecision.Approved || signal.Action == SignalAction.Hold)
        {
            return new StrategyExecutionResult
            {
                Signal = signal,
                RiskDecision = riskDecision,
                OrderResult = null
            };
        }

        var order = new OrderRequest
        {
            Instrument = signal.Instrument,
            Type = OrderType.Market,
            Side = signal.Action == SignalAction.Buy ? TradeSide.Buy : TradeSide.Sell,
            Units = signal.Units,
            Price = tick.Mid,
            StopLoss = signal.StopLoss,
            TakeProfit = signal.TakeProfit,
            ClientOrderId = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow
        };

        await _audit.AppendAsync("order-request", order, ct);
        var result = await _broker.PlaceOrderAsync(order, ct);
        await _audit.AppendAsync("order-result", result, ct);

        if (result.State == OrderState.Filled)
            _tradeCountsByInstrument.AddOrUpdate(request.Instrument, 1, (_, current) => current + 1);

        return new StrategyExecutionResult
        {
            Signal = signal,
            RiskDecision = riskDecision,
            OrderResult = result
        };
    }

    public async Task EngageKillSwitchAsync(bool closePositions, CancellationToken ct)
    {
        await _killSwitch.SetStateAsync(KillSwitchState.Engaged, ct);
        await _audit.AppendAsync("kill-switch", new { state = "engaged", closePositions }, ct);
        if (closePositions)
            await _broker.EngageKillSwitchCloseAllAsync(ct);
    }

    private static Signal ResizeSignalToRisk(
        Signal signal,
        AccountSnapshot account,
        PriceTick tick,
        decimal maxRiskPerTradeFraction)
    {
        if (signal.Action == SignalAction.Hold || signal.StopLoss is null)
            return signal;

        var entry = tick.Mid;
        var stop = signal.StopLoss.Value;
        var stopDistance = Math.Abs(entry - stop);

        if (stopDistance <= 0 || account.Equity <= 0 || maxRiskPerTradeFraction <= 0)
        {
            return signal with
            {
                Entry = entry,
                Units = 0,
                Reason = $"{signal.Reason} | Unable to size position safely"
            };
        }

        var maxRiskDollars = account.Equity * maxRiskPerTradeFraction;
        var sizedUnits = Math.Floor(maxRiskDollars / stopDistance);

        if (sizedUnits < 1)
            sizedUnits = 1;

        return signal with
        {
            Entry = entry,
            Units = sizedUnits,
            Reason = $"{signal.Reason} | Risk-sized units={sizedUnits}"
        };
    }
}
