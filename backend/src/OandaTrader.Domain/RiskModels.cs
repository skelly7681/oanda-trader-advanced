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
