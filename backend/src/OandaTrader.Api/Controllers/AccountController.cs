using Microsoft.AspNetCore.Mvc;
using OandaTrader.Application;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private readonly IBrokerGateway _broker;

    public AccountController(IBrokerGateway broker)
    {
        _broker = broker;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await _broker.GetAccountSnapshotAsync(ct));
}