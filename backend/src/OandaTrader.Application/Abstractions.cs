using OandaTrader.Domain;

namespace OandaTrader.Application;

public interface IStrategy
{
    string Name { get; }
    void OnTick(PriceTick tick);
    void OnBar(Candle bar);
    Signal GenerateSignal(StrategyContext context);
}

public interface IBrokerGateway
{
    TradingMode Mode { get; }
    Task<AccountSnapshot> GetAccountSnapshotAsync(CancellationToken ct);
    Task<IReadOnlyList<PriceTick>> GetLatestPricesAsync(IEnumerable<string> instruments, CancellationToken ct);
    Task<OrderResult> PlaceOrderAsync(OrderRequest request, CancellationToken ct);
    Task<IReadOnlyList<OpenTrade>> GetOpenTradesAsync(CancellationToken ct);
    Task CloseTradeAsync(string tradeId, CancellationToken ct);
    Task EngageKillSwitchCloseAllAsync(CancellationToken ct);
}

public interface IMarketDataClient
{
    Task<IReadOnlyList<Candle>> GetCandlesAsync(string instrument, string granularity, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
}

public interface IAuditStore
{
    Task AppendAsync(string eventType, object payload, CancellationToken ct);
    Task<IReadOnlyList<string>> GetRecentAsync(int count, CancellationToken ct);
}

public interface IKillSwitchStore
{
    Task<KillSwitchState> GetStateAsync(CancellationToken ct);
    Task SetStateAsync(KillSwitchState state, CancellationToken ct);
}

public interface IStrategyRegistry
{
    IReadOnlyList<string> Names { get; }
    IStrategy Resolve(string name);
}