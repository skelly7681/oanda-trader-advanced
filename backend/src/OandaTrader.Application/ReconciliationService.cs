using OandaTrader.Domain;

namespace OandaTrader.Application;

public sealed class ReconciliationService
{
    private readonly IBrokerGateway _broker;
    private readonly IAuditStore _audit;

    public ReconciliationService(IBrokerGateway broker, IAuditStore audit)
    {
        _broker = broker;
        _audit = audit;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var trades = await _broker.GetOpenTradesAsync(ct);
        await _audit.AppendAsync("reconciliation", new { openTradeCount = trades.Count, ranAt = DateTimeOffset.UtcNow }, ct);
    }
}