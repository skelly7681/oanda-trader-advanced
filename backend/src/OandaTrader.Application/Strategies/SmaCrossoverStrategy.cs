using OandaTrader.Domain;

namespace OandaTrader.Application.Strategies;

public sealed class SmaCrossoverStrategy : IStrategy
{
    public string Name => "sma-crossover";

    public void OnTick(PriceTick tick) { }
    public void OnBar(Candle bar) { }

    public Signal GenerateSignal(StrategyContext context)
    {
        var closes = context.Candles.Select(c => c.Close).ToList();
        if (closes.Count < 25)
            return Hold(context, "Not enough candles");

        decimal fast = closes.TakeLast(10).Average();
        decimal slow = closes.TakeLast(20).Average();

        var entry = context.CurrentTick?.Mid ?? closes.Last();

        if (fast > slow)
        {
            var stop = entry - (entry * 0.0025m);
            var tp = entry + (entry * 0.005m);

            return new Signal(
                Name,
                context.Instrument,
                SignalAction.Buy,
                entry,
                stop,
                tp,
                1000m,
                "Fast SMA > Slow SMA",
                context.Now);
        }

        if (fast < slow)
        {
            var stop = entry + (entry * 0.0025m);
            var tp = entry - (entry * 0.005m);

            return new Signal(
                Name,
                context.Instrument,
                SignalAction.Sell,
                entry,
                stop,
                tp,
                1000m,
                "Fast SMA < Slow SMA",
                context.Now);
        }

        return Hold(context, "No crossover edge");
    }

    private Signal Hold(StrategyContext context, string reason)
        => new(Name, context.Instrument, SignalAction.Hold, null, null, null, 0, reason, context.Now);
}