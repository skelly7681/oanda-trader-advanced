namespace OandaTrader.Domain;

public sealed record Candle(
    string Instrument,
    string Granularity,
    DateTimeOffset Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume,
    bool Complete);