namespace OandaTrader.Domain;

public sealed record Signal(
    string Strategy,
    string Instrument,
    SignalAction Action,
    decimal? Entry,
    decimal? StopLoss,
    decimal? TakeProfit,
    decimal Units,
    string Reason,
    DateTimeOffset Timestamp);