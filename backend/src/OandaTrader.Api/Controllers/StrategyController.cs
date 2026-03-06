using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OandaTrader.Api.Models;
using OandaTrader.Application;
using OandaTrader.Domain;
using OandaTrader.Infrastructure.Configuration;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/strategy")]
public sealed class StrategyController : ControllerBase
{
    private readonly TradingEngine _engine;
    private readonly IStrategyRegistry _strategies;
    private readonly TradingOptions _options;

    public StrategyController(TradingEngine engine, IStrategyRegistry strategies, IOptions<TradingOptions> options)
    {
        _engine = engine;
        _strategies = strategies;
        _options = options.Value;
    }

    [HttpGet("list")]
    public IActionResult List() => Ok(_strategies.Names);

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] RunStrategyDto dto, CancellationToken ct)
    {
        var req = new StrategyRunRequest
        {
            StrategyName = dto.StrategyName,
            Instrument = dto.Instrument,
            Granularity = dto.Granularity,
            Mode = Enum.Parse<TradingMode>(dto.Mode, ignoreCase: true),
            AcceptLiveRisk = dto.AcceptLiveRisk
        };

        var result = await _engine.EvaluateAndMaybeTradeAsync(
            req,
            _options.MaxRiskPerTradeFraction,
            _options.MaxDailyLossFraction,
            _options.MaxTradesPerDayPerInstrument,
            _options.MaxSpread,
            _options.MaxLeverage,
            _options.StopLossRequired,
            ct);

        return Ok(result);
    }
}