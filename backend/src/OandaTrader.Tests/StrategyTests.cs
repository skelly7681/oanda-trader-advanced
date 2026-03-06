using System;
using System.Linq;
using Xunit;
using OandaTrader.Application.Strategies;
using OandaTrader.Domain;

namespace OandaTrader.Tests;

public sealed class StrategyTests
{
    [Fact]
    public void Sma_Crossover_Returns_Buy_When_Fast_Above_Slow()
    {
        var strategy = new SmaCrossoverStrategy();
        var candles = Enumerable.Range(1, 30)
            .Select(i => new Candle("EUR_USD", "M5", DateTimeOffset.UtcNow.AddMinutes(i), 1, 1, 1, 1 + i * 0.001m, 100, true))
            .ToList();

        var signal = strategy.GenerateSignal(new StrategyContext
        {
            Instrument = "EUR_USD",
            Granularity = "M5",
            Candles = candles,
            Now = DateTimeOffset.UtcNow
        });

        Assert.Equal(SignalAction.Buy, signal.Action);
    }
}