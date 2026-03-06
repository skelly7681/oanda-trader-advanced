using OandaTrader.Domain;

namespace OandaTrader.Application;

public sealed class BacktestService
{
    private readonly IMarketDataClient _marketData;
    private readonly IStrategyRegistry _strategies;

    public BacktestService(IMarketDataClient marketData, IStrategyRegistry strategies)
    {
        _marketData = marketData;
        _strategies = strategies;
    }

    public async Task<object> RunAsync(string strategyName, string instrument, string granularity, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var candles = await _marketData.GetCandlesAsync(instrument, granularity, from, to, ct);
        var strategy = _strategies.Resolve(strategyName);

        int trades = 0;
        int wins = 0;
        decimal pnl = 0;

        for (int i = 30; i < candles.Count; i++)
        {
            var window = candles.Take(i + 1).ToList();
            var context = new StrategyContext
            {
                Instrument = instrument,
                Granularity = granularity,
                Candles = window,
                CurrentTick = null,
                Now = window.Last().Timestamp
            };

            var signal = strategy.GenerateSignal(context);
            if (signal.Action == SignalAction.Hold || signal.Entry is null || signal.TakeProfit is null || signal.StopLoss is null)
                continue;

            trades++;
            var rr = Math.Abs(signal.TakeProfit.Value - signal.Entry.Value) / Math.Abs(signal.Entry.Value - signal.StopLoss.Value);
            if (rr >= 2)
            {
                wins++;
                pnl += 2;
            }
            else
            {
                pnl -= 1;
            }
        }

        return new
        {
            strategy = strategyName,
            instrument,
            granularity,
            from,
            to,
            tradeCount = trades,
            winRate = trades == 0 ? 0 : (decimal)wins / trades,
            netR = pnl
        };
    }
}