using OandaTrader.Domain;

namespace OandaTrader.Application.Strategies;

public sealed class BreakoutAtrStrategy : IStrategy
{
    public string Name => "breakout-atr";

    public void OnTick(PriceTick tick) { }
    public void OnBar(Candle bar) { }

    public Signal GenerateSignal(StrategyContext context)
    {
        var candles = context.Candles.ToList();
        if (candles.Count < 30)
            return Hold(context, "Not enough candles");

        var last = candles.Last();
        var rangeWindow = candles.TakeLast(20).ToList();
        var breakoutHigh = rangeWindow.Max(x => x.High);
        var breakoutLow = rangeWindow.Min(x => x.Low);
        var atr = CalcAtr(candles.TakeLast(15).ToList());

        if (last.Close >= breakoutHigh)
        {
            var entry = last.Close;
            var stop = entry - (atr * 1.5m);
            var tp = entry + (atr * 3m);
            return new Signal(Name, context.Instrument, SignalAction.Buy, entry, stop, tp, 1000m, "Upside breakout with ATR stop", context.Now);
        }

        if (last.Close <= breakoutLow)
        {
            var entry = last.Close;
            var stop = entry + (atr * 1.5m);
            var tp = entry - (atr * 3m);
            return new Signal(Name, context.Instrument, SignalAction.Sell, entry, stop, tp, 1000m, "Downside breakout with ATR stop", context.Now);
        }

        return Hold(context, "No breakout");
    }

    private static decimal CalcAtr(List<Candle> candles)
    {
        if (candles.Count < 2) return 0;
        var trs = new List<decimal>();
        for (int i = 1; i < candles.Count; i++)
        {
            var cur = candles[i];
            var prev = candles[i - 1];
            var tr = new[]
            {
                cur.High - cur.Low,
                Math.Abs(cur.High - prev.Close),
                Math.Abs(cur.Low - prev.Close)
            }.Max();
            trs.Add(tr);
        }
        return trs.Count == 0 ? 0 : trs.Average();
    }

    private Signal Hold(StrategyContext context, string reason)
        => new(Name, context.Instrument, SignalAction.Hold, null, null, null, 0, reason, context.Now);
}