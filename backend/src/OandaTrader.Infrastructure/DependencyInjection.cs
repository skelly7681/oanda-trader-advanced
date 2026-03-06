using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OandaTrader.Application;
using OandaTrader.Domain;
using OandaTrader.Infrastructure.Brokers;
using OandaTrader.Infrastructure.Configuration;
using OandaTrader.Infrastructure.MarketData;
using OandaTrader.Infrastructure.Persistence;

namespace OandaTrader.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<TradingOptions>(config.GetSection("Trading"));
        services.Configure<OandaOptions>(config.GetSection("Oanda"));

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<OandaOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<TradingOptions>>().Value);

        services.AddSingleton<IAuditStore, FileAuditStore>();
        services.AddSingleton<IKillSwitchStore, FileKillSwitchStore>();
        services.AddSingleton<InMemoryBrokerState>();
        services.AddSingleton<IStrategyRegistry, InMemoryStrategyRegistry>();

        services.AddHttpClient<OandaBrokerGateway>();
        services.AddHttpClient<OandaMarketDataClient>();

        services.AddSingleton<PaperBrokerGateway>();
        services.AddSingleton<SimulatedMarketDataClient>();

        services.AddSingleton<IBrokerGateway>(sp =>
        {
            var env = sp.GetRequiredService<OandaOptions>().Environment;
            return env.Equals("paper", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<PaperBrokerGateway>()
                : sp.GetRequiredService<OandaBrokerGateway>();
        });

        services.AddSingleton<IMarketDataClient>(sp =>
        {
            var env = sp.GetRequiredService<OandaOptions>().Environment;
            return env.Equals("paper", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<SimulatedMarketDataClient>()
                : sp.GetRequiredService<OandaMarketDataClient>();
        });

        services.AddSingleton<RiskGate>();
        services.AddSingleton<TradingEngine>();
        services.AddSingleton<ReconciliationService>();
        services.AddSingleton<BacktestService>();

        return services;
    }
}