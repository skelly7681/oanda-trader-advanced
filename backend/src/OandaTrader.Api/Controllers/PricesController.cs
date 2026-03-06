using Microsoft.AspNetCore.Mvc;
using OandaTrader.Application;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/prices")]
public sealed class PricesController : ControllerBase
{
    private readonly IBrokerGateway _broker;

    public PricesController(IBrokerGateway broker)
    {
        _broker = broker;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var prices = await _broker.GetLatestPricesAsync(new[] { "EUR_USD", "GBP_USD", "XAU_USD" }, ct);
        return Ok(prices);
    }
}