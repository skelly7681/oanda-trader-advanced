using Microsoft.AspNetCore.SignalR;
using OandaTrader.Api.Hubs;
using OandaTrader.Application;

namespace OandaTrader.Api.Services;

public sealed class DashboardBroadcaster
{
    private readonly IHubContext<TradingHub> _hub;
    private readonly IBrokerGateway _broker;
    private readonly IAuditStore _audit;

    public DashboardBroadcaster(IHubContext<TradingHub> hub, IBrokerGateway broker, IAuditStore audit)
    {
        _hub = hub;
        _broker = broker;
        _audit = audit;
    }

    public async Task BroadcastSnapshotAsync(CancellationToken ct)
    {
        var account = await _broker.GetAccountSnapshotAsync(ct);
        var prices = await _broker.GetLatestPricesAsync(new[] { "EUR_USD", "GBP_USD", "XAU_USD" }, ct);
        var trades = await _broker.GetOpenTradesAsync(ct);
        var logs = await _audit.GetRecentAsync(50, ct);

        await _hub.Clients.All.SendAsync("snapshot", new
        {
            account,
            prices,
            trades,
            logs,
            timestamp = DateTimeOffset.UtcNow
        }, ct);
    }
}