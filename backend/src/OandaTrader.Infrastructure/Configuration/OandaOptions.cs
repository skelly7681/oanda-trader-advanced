namespace OandaTrader.Infrastructure.Configuration;

public sealed class OandaOptions
{
    public string Environment { get; set; } = "practice";
    public string AccountId { get; set; } = "";
    public string Token { get; set; } = "";
    public string PracticeRestBaseUrl { get; set; } = "https://api-fxpractice.oanda.com";
    public string LiveRestBaseUrl { get; set; } = "https://api-fxtrade.oanda.com";
}