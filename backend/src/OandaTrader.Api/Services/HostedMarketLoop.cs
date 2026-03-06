using OandaTrader.Application;

namespace OandaTrader.Api.Services;

public sealed class HostedMarketLoop : BackgroundService
{
    private readonly DashboardBroadcaster _broadcaster;
    private readonly ReconciliationService _reconciliation;
    private readonly ILogger<HostedMarketLoop> _logger;

    public HostedMarketLoop(DashboardBroadcaster broadcaster, ReconciliationService reconciliation, ILogger<HostedMarketLoop> logger)
    {
        _broadcaster = broadcaster;
        _reconciliation = reconciliation;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _broadcaster.BroadcastSnapshotAsync(stoppingToken);
                await _reconciliation.RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hosted market loop failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
}