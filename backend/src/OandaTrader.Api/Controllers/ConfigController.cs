using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OandaTrader.Api.Models;
using OandaTrader.Infrastructure.Configuration;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/config")]
public sealed class ConfigController : ControllerBase
{
    private readonly TradingOptions _options;

    public ConfigController(IOptions<TradingOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet]
    public IActionResult Get()
        => Ok(new TradingConfigDto
        {
            DefaultInstrument = _options.DefaultInstrument,
            DefaultGranularity = _options.DefaultGranularity,
            MaxRiskPerTradeFraction = _options.MaxRiskPerTradeFraction,
            MaxDailyLossFraction = _options.MaxDailyLossFraction,
            MaxTradesPerDayPerInstrument = _options.MaxTradesPerDayPerInstrument,
            MaxSpread = _options.MaxSpread,
            MaxLeverage = _options.MaxLeverage,
            StopLossRequired = _options.StopLossRequired,
            ClosePositionsOnKillSwitch = _options.ClosePositionsOnKillSwitch
        });
}