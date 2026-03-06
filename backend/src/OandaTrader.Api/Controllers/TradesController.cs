using Microsoft.AspNetCore.Mvc;
using OandaTrader.Application;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/trades")]
public sealed class TradesController : ControllerBase
{
    private readonly IBrokerGateway _broker;

    public TradesController(IBrokerGateway broker)
    {
        _broker = broker;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await _broker.GetOpenTradesAsync(ct));

    [HttpDelete("{tradeId}")]
    public async Task<IActionResult> Close(string tradeId, CancellationToken ct)
    {
        await _broker.CloseTradeAsync(tradeId, ct);
        return NoContent();
    }
}