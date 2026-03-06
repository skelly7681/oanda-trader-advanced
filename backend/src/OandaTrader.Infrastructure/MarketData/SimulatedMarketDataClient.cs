using OandaTrader.Application;
using OandaTrader.Domain;

namespace OandaTrader.Infrastructure.MarketData;

public sealed class SimulatedMarketDataClient : IMarketDataClient
{
    public Task<IReadOnlyList<Candle>> GetCandlesAsync(string instrument, string granularity, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var list = new List<Candle>();
        var rand = new Random(42);
        decimal price = instrument == "XAU_USD" ? 2100m : 1.10m;
        var t = from;
        while (t < to)
        {
            var drift = (decimal)(rand.NextDouble() - 0.5) * (instrument == "XAU_USD" ? 10m : 0.01m);
            var open = price;
            var close = price + drift;
            var high = Math.Max(open, close) + Math.Abs(drift * 0.5m);
            var low = Math.Min(open, close) - Math.Abs(drift * 0.5m);
            list.Add(new Candle(instrument, granularity, t, open, high, low, close, 1000, true));
            price = close;
            t = granularity switch
            {
                "M1" => t.AddMinutes(1),
                "M5" => t.AddMinutes(5),
                "H1" => t.AddHours(1),
                _ => t.AddMinutes(5)
            };
        }
        return Task.FromResult<IReadOnlyList<Candle>>(list);
    }
}