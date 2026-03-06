namespace OandaTrader.Domain;

public sealed class StrategyContext
{
    public required string Instrument { get; init; }
    public required string Granularity { get; init; }
    public required IReadOnlyList<Candle> Candles { get; init; }
    public PriceTick? CurrentTick { get; init; }
    public required DateTimeOffset Now { get; init; }
}