using OandaTrader.Domain;

namespace OandaTrader.Application;

public sealed class RiskGate
{
    public RiskDecision Evaluate(
        AccountSnapshot account,
        Signal signal,
        PriceTick tick,
        int tradesTodayForInstrument,
        decimal maxRiskPerTradeFraction,
        decimal maxDailyLossFraction,
        int maxTradesPerDayPerInstrument,
        decimal maxSpread,
        decimal maxLeverage,
        bool stopLossRequired)
    {
        if (signal.Action == SignalAction.Hold)
            return new(true, "Hold signal does not require blocking.");

        if (account.DrawdownFraction >= 0.35m)
            return new(false, "Drawdown hard stop triggered at 35%.");

        if (account.StartingBalance > 0 && Math.Abs(account.DailyPnL) / account.StartingBalance >= maxDailyLossFraction && account.DailyPnL < 0)
            return new(false, "Daily loss limit exceeded.");

        if (tradesTodayForInstrument >= maxTradesPerDayPerInstrument)
            return new(false, "Trade count limit exceeded for instrument.");

        if (tick.Spread > maxSpread)
            return new(false, $"Spread too wide: {tick.Spread}");

        if (stopLossRequired && signal.StopLoss is null)
            return new(false, "Stop loss is required.");

        if (signal.Entry is null || signal.StopLoss is null)
            return new(false, "Signal is missing entry or stop loss.");

        var stopDistance = Math.Abs(signal.Entry.Value - signal.StopLoss.Value);
        if (stopDistance <= 0)
            return new(false, "Stop distance must be positive.");

        var riskDollars = stopDistance * signal.Units;
        var riskFraction = account.Equity <= 0 ? 1m : riskDollars / account.Equity;
        if (riskFraction > maxRiskPerTradeFraction)
            return new(false, $"Risk per trade exceeds threshold: {riskFraction:P2}");

        var leverage = account.Equity <= 0 || signal.Entry.Value <= 0 ? decimal.MaxValue : (signal.Units * signal.Entry.Value) / account.Equity;
        if (leverage > maxLeverage)
            return new(false, $"Leverage exceeds threshold: {leverage:N2}");

        return new(true, "Approved");
    }
}