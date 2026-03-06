using Microsoft.AspNetCore.Mvc;
using OandaTrader.Api.Models;
using OandaTrader.Application;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/backtest")]
public sealed class BacktestController : ControllerBase
{
    private readonly BacktestService _backtests;

    public BacktestController(BacktestService backtests)
    {
        _backtests = backtests;
    }

    [HttpPost]
    public async Task<IActionResult> Run([FromBody] BacktestDto dto, CancellationToken ct)
        => Ok(await _backtests.RunAsync(dto.StrategyName, dto.Instrument, dto.Granularity, dto.From, dto.To, ct));
}