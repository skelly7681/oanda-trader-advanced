using System;
using Xunit;
using OandaTrader.Application;
using OandaTrader.Domain;

namespace OandaTrader.Tests;

public sealed class RiskGateTests
{
    [Fact]
    public void Blocks_When_Spread_Too_High()
    {
        var gate = new RiskGate();
        var account = MakeAccount();
        var signal = new Signal("test", "XAU_USD", SignalAction.Buy, 2000m, 1995m, 2010m, 1m, "x", DateTimeOffset.UtcNow);
        var tick = new PriceTick("XAU_USD", 2000m, 2001m, DateTimeOffset.UtcNow);

        var result = gate.Evaluate(account, signal, tick, 0, 0.01m, 0.03m, 3, 0.50m, 5m, true);
        Assert.False(result.Approved);
    }

    [Fact]
    public void Blocks_When_Max_Trades_Exceeded()
    {
        var gate = new RiskGate();
        var account = MakeAccount();
        var signal = new Signal("test", "EUR_USD", SignalAction.Buy, 1.10m, 1.095m, 1.11m, 1000m, "x", DateTimeOffset.UtcNow);
        var tick = new PriceTick("EUR_USD", 1.10m, 1.1001m, DateTimeOffset.UtcNow);

        var result = gate.Evaluate(account, signal, tick, 3, 0.01m, 0.03m, 3, 0.001m, 5m, true);
        Assert.False(result.Approved);
    }

    private static AccountSnapshot MakeAccount() => new()
    {
        AccountId = "A",
        Balance = 10000m,
        Equity = 10000m,
        MarginAvailable = 10000m,
        MarginUsed = 0m,
        StartingBalance = 10000m,
        DailyPnL = 0m,
        UnrealizedPnL = 0m,
        OpenTrades = new(),
        Positions = new()
    };
}