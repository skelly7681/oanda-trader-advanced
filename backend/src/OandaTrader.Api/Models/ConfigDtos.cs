namespace OandaTrader.Api.Models;

public sealed class TradingConfigDto
{
    public string DefaultInstrument { get; set; } = "XAU_USD";
    public string DefaultGranularity { get; set; } = "M5";
    public decimal MaxRiskPerTradeFraction { get; set; } = 0.01m;
    public decimal MaxDailyLossFraction { get; set; } = 0.03m;
    public int MaxTradesPerDayPerInstrument { get; set; } = 3;
    public decimal MaxSpread { get; set; } = 0.50m;
    public decimal MaxLeverage { get; set; } = 5m;
    public bool StopLossRequired { get; set; } = true;
    public bool ClosePositionsOnKillSwitch { get; set; } = false;
}