namespace OandaTrader.Api.Models;

public sealed class RunStrategyDto
{
    public required string StrategyName { get; init; }
    public required string Instrument { get; init; }
    public required string Granularity { get; init; }
    public required string Mode { get; init; }
    public bool AcceptLiveRisk { get; init; }
}

public sealed class KillSwitchDto
{
    public bool ClosePositions { get; init; }
}

public sealed class BacktestDto
{
    public required string StrategyName { get; init; }
    public required string Instrument { get; init; }
    public required string Granularity { get; init; }
    public required DateTimeOffset From { get; init; }
    public required DateTimeOffset To { get; init; }
}