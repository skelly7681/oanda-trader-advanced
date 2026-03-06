namespace OandaTrader.Domain;

public sealed class AccountSnapshot
{
    public required string AccountId { get; init; }
    public decimal Balance { get; set; }
    public decimal Equity { get; set; }
    public decimal MarginUsed { get; set; }
    public decimal MarginAvailable { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal DailyPnL { get; set; }
    public decimal StartingBalance { get; set; }
    public decimal DrawdownFraction => StartingBalance <= 0 ? 0 : Math.Max(0, (StartingBalance - Equity) / StartingBalance);
    public List<OpenTrade> OpenTrades { get; init; } = new();
    public List<PositionSnapshot> Positions { get; init; } = new();
}