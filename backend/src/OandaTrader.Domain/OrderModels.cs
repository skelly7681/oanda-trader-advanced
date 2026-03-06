namespace OandaTrader.Domain;

public sealed class OrderRequest
{
    public required string Instrument { get; init; }
    public required OrderType Type { get; init; }
    public required TradeSide Side { get; init; }
    public required decimal Units { get; init; }
    public decimal? Price { get; init; }
    public decimal? StopLoss { get; init; }
    public decimal? TakeProfit { get; init; }
    public bool UseTrailingStop { get; init; }
    public decimal? TrailingStopDistance { get; init; }
    public required string ClientOrderId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed class OrderResult
{
    public required string OrderId { get; init; }
    public required OrderState State { get; init; }
    public string? BrokerTradeId { get; init; }
    public string? BrokerMessage { get; init; }
    public decimal? FillPrice { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

public sealed class OpenTrade
{
    public required string TradeId { get; init; }
    public required string Instrument { get; init; }
    public required TradeSide Side { get; init; }
    public required decimal Units { get; init; }
    public required decimal EntryPrice { get; init; }
    public decimal? StopLoss { get; init; }
    public decimal? TakeProfit { get; init; }
    public required decimal UnrealizedPnL { get; init; }
    public required DateTimeOffset OpenedAt { get; init; }
}

public sealed class PositionSnapshot
{
    public required string Instrument { get; init; }
    public decimal LongUnits { get; init; }
    public decimal ShortUnits { get; init; }
    public decimal NetUnits => LongUnits - ShortUnits;
    public decimal UnrealizedPnL { get; init; }
}