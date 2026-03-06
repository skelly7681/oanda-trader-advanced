using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OandaTrader.Api.Models;
using OandaTrader.Application;
using OandaTrader.Infrastructure.Configuration;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/kill-switch")]
public sealed class KillSwitchController : ControllerBase
{
    private readonly TradingEngine _engine;
    private readonly TradingOptions _options;

    public KillSwitchController(TradingEngine engine, IOptions<TradingOptions> options)
    {
        _engine = engine;
        _options = options.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Engage([FromBody] KillSwitchDto dto, CancellationToken ct)
    {
        await _engine.EngageKillSwitchAsync(dto.ClosePositions || _options.ClosePositionsOnKillSwitch, ct);
        return Ok(new { status = "engaged" });
    }
}