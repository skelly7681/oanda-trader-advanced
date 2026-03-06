using OandaTrader.Application;
using OandaTrader.Domain;
using OandaTrader.Application.Strategies;

namespace OandaTrader.Infrastructure.Persistence;

public sealed class InMemoryStrategyRegistry : IStrategyRegistry
{
    private readonly Dictionary<string, IStrategy> _strategies;

    public InMemoryStrategyRegistry()
    {
        _strategies = new(StringComparer.OrdinalIgnoreCase)
        {
            ["sma-crossover"] = new SmaCrossoverStrategy(),
            ["breakout-atr"] = new BreakoutAtrStrategy()
        };
    }

    public IReadOnlyList<string> Names => _strategies.Keys.OrderBy(x => x).ToArray();

    public IStrategy Resolve(string name)
        => _strategies.TryGetValue(name, out var strategy)
            ? strategy
            : throw new InvalidOperationException($"Unknown strategy '{name}'");
}

public sealed class InMemoryBrokerState
{
    public AccountSnapshot Account { get; set; } = new()
    {
        AccountId = "PAPER-001",
        Balance = 10_000m,
        Equity = 10_000m,
        MarginAvailable = 10_000m,
        MarginUsed = 0m,
        StartingBalance = 10_000m,
        UnrealizedPnL = 0m,
        DailyPnL = 0m,
        OpenTrades = new(),
        Positions = new()
    };
}