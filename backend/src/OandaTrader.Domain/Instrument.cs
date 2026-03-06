namespace OandaTrader.Domain;

public sealed record Instrument(string Symbol, int DisplayPrecision, decimal PipSize)
{
    public static readonly Instrument EurUsd = new("EUR_USD", 5, 0.0001m);
    public static readonly Instrument GbpUsd = new("GBP_USD", 5, 0.0001m);
    public static readonly Instrument XauUsd = new("XAU_USD", 3, 0.01m);

    public static IReadOnlyList<Instrument> Supported => new[] { EurUsd, GbpUsd, XauUsd };
}