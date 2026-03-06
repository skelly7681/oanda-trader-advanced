#!/usr/bin/env bash
set -euo pipefail

echo "Patching trading execution flow..."

cat > backend/src/OandaTrader.Domain/RiskModels.cs <<'CSHARP'
namespace OandaTrader.Domain;

public sealed record RiskDecision(bool Approved, string Reason);

public sealed class StrategyRunRequest
{
    public required string StrategyName { get; init; }
    public required string Instrument { get; init; }
    public required string Granularity { get; init; }
    public required TradingMode Mode { get; init; }
    public bool AcceptLiveRisk { get; init; }
}

public sealed class StrategyExecutionResult
{
    public required Signal Signal { get; init; }
    public required RiskDecision RiskDecision { get; init; }
    public OrderResult? OrderResult { get; init; }
    public bool TradeSubmitted => OrderResult is not null;
    public bool TradeFilled => OrderResult?.State == OrderState.Filled;
}
CSHARP

cat > backend/src/OandaTrader.Application/TradingEngine.cs <<'CSHARP'
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
CSHARP

cat > backend/src/OandaTrader.Infrastructure/Brokers/PaperBrokerGateway.cs <<'CSHARP'
using OandaTrader.Application;
using OandaTrader.Domain;
using OandaTrader.Infrastructure.Persistence;

namespace OandaTrader.Infrastructure.Brokers;

public sealed class PaperBrokerGateway : IBrokerGateway
{
    private readonly InMemoryBrokerState _state;
    public TradingMode Mode => TradingMode.Paper;

    public PaperBrokerGateway(InMemoryBrokerState state)
    {
        _state = state;
    }

    public Task<AccountSnapshot> GetAccountSnapshotAsync(CancellationToken ct)
        => Task.FromResult(_state.Account);

    public Task<IReadOnlyList<PriceTick>> GetLatestPricesAsync(IEnumerable<string> instruments, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var list = instruments.Select(i => i switch
        {
            "EUR_USD" => new PriceTick(i, 1.0830m, 1.0831m, now),
            "GBP_USD" => new PriceTick(i, 1.2710m, 1.2712m, now),
            "XAU_USD" => new PriceTick(i, 2148.20m, 2148.60m, now),
            _ => new PriceTick(i, 1m, 1.01m, now)
        }).ToList();

        return Task.FromResult<IReadOnlyList<PriceTick>>(list);
    }

    public Task<OrderResult> PlaceOrderAsync(OrderRequest request, CancellationToken ct)
    {
        var fillPrice = request.Price ?? request.Instrument switch
        {
            "EUR_USD" => 1.0831m,
            "GBP_USD" => 1.2712m,
            "XAU_USD" => 2148.60m,
            _ => 1m
        };

        var trade = new OpenTrade
        {
            TradeId = Guid.NewGuid().ToString("N"),
            Instrument = request.Instrument,
            Side = request.Side,
            Units = request.Units,
            EntryPrice = fillPrice,
            StopLoss = request.StopLoss,
            TakeProfit = request.TakeProfit,
            UnrealizedPnL = 0m,
            OpenedAt = DateTimeOffset.UtcNow
        };

        _state.Account.OpenTrades.Add(trade);
        _state.Account.MarginUsed += fillPrice * request.Units * 0.02m;
        _state.Account.MarginAvailable = _state.Account.Equity - _state.Account.MarginUsed;

        return Task.FromResult(new OrderResult
        {
            OrderId = Guid.NewGuid().ToString("N"),
            BrokerTradeId = trade.TradeId,
            State = OrderState.Filled,
            BrokerMessage = "Paper fill",
            FillPrice = fillPrice,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public Task<IReadOnlyList<OpenTrade>> GetOpenTradesAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<OpenTrade>>(_state.Account.OpenTrades.ToList());

    public Task CloseTradeAsync(string tradeId, CancellationToken ct)
    {
        _state.Account.OpenTrades.RemoveAll(x => x.TradeId == tradeId);
        return Task.CompletedTask;
    }

    public Task EngageKillSwitchCloseAllAsync(CancellationToken ct)
    {
        _state.Account.OpenTrades.Clear();
        _state.Account.MarginUsed = 0m;
        _state.Account.MarginAvailable = _state.Account.Equity;
        return Task.CompletedTask;
    }
}
CSHARP

cat > frontend/src/components/StrategyPanel.tsx <<'TSX'
import { useEffect, useState } from 'react'
import { getJson, postJson } from '../api'

export function StrategyPanel() {
  const [strategies, setStrategies] = useState<string[]>([])
  const [strategyName, setStrategyName] = useState('sma-crossover')
  const [instrument, setInstrument] = useState('XAU_USD')
  const [granularity, setGranularity] = useState('M5')
  const [mode, setMode] = useState('Paper')
  const [result, setResult] = useState<string>('')

  useEffect(() => {
    getJson<string[]>('/strategy/list').then(setStrategies)
  }, [])

  async function run() {
    const data = await postJson<any>('/strategy/run', {
      strategyName,
      instrument,
      granularity,
      mode,
      acceptLiveRisk: false
    })
    setResult(JSON.stringify(data, null, 2))
  }

  return (
    <div className="card">
      <h3>Strategy Runner</h3>
      <div className="grid two">
        <label>
          Strategy
          <select value={strategyName} onChange={e => setStrategyName(e.target.value)}>
            {strategies.map(s => <option key={s} value={s}>{s}</option>)}
          </select>
        </label>
        <label>
          Instrument
          <select value={instrument} onChange={e => setInstrument(e.target.value)}>
            <option>EUR_USD</option>
            <option>GBP_USD</option>
            <option>XAU_USD</option>
          </select>
        </label>
        <label>
          Granularity
          <select value={granularity} onChange={e => setGranularity(e.target.value)}>
            <option>M1</option>
            <option>M5</option>
            <option>H1</option>
          </select>
        </label>
        <label>
          Mode
          <select value={mode} onChange={e => setMode(e.target.value)}>
            <option>Paper</option>
            <option>Practice</option>
            <option>Live</option>
          </select>
        </label>
      </div>
      <button onClick={run}>Run Strategy</button>
      <p>
        Result now includes the strategy signal, the risk decision, and any order result.
      </p>
      <pre>{result}</pre>
    </div>
  )
}
TSX

echo "Patch complete."
echo "Now run:"
echo "  dotnet build"
echo "  dotnet test"
echo "  dotnet run --project backend/src/OandaTrader.Api"
echo "  (in another terminal) cd frontend && npm run dev"
