namespace OandaTrader.Domain;

public sealed record PriceTick(
    string Instrument,
    decimal Bid,
    decimal Ask,
    DateTimeOffset Timestamp)
{
    public decimal Mid => (Bid + Ask) / 2m;
    public decimal Spread => Ask - Bid;
}