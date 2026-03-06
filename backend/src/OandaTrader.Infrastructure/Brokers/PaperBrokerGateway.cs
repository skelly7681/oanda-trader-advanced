using OandaTrader.Application;
using OandaTrader.Domain;
using OandaTrader.Infrastructure.Persistence;

namespace OandaTrader.Infrastructure.Brokers;

public sealed class PaperBrokerGateway : IBrokerGateway
{
    private readonly InMemoryBrokerState _state;
    public TradingMode Mode => TradingMode.Paper;

    public PaperBrokerGateway(InMemoryBrokerState state)
    {
        _state = state;
    }

    public Task<AccountSnapshot> GetAccountSnapshotAsync(CancellationToken ct)
        => Task.FromResult(_state.Account);

    public Task<IReadOnlyList<PriceTick>> GetLatestPricesAsync(IEnumerable<string> instruments, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var list = instruments.Select(i => i switch
        {
            "EUR_USD" => new PriceTick(i, 1.0830m, 1.0831m, now),
            "GBP_USD" => new PriceTick(i, 1.2710m, 1.2712m, now),
            "XAU_USD" => new PriceTick(i, 2148.20m, 2148.60m, now),
            _ => new PriceTick(i, 1m, 1.01m, now)
        }).ToList();

        return Task.FromResult<IReadOnlyList<PriceTick>>(list);
    }

    public Task<OrderResult> PlaceOrderAsync(OrderRequest request, CancellationToken ct)
    {
        var fillPrice = request.Price ?? request.Instrument switch
        {
            "EUR_USD" => 1.0831m,
            "GBP_USD" => 1.2712m,
            "XAU_USD" => 2148.60m,
            _ => 1m
        };

        var trade = new OpenTrade
        {
            TradeId = Guid.NewGuid().ToString("N"),
            Instrument = request.Instrument,
            Side = request.Side,
            Units = request.Units,
            EntryPrice = fillPrice,
            StopLoss = request.StopLoss,
            TakeProfit = request.TakeProfit,
            UnrealizedPnL = 0m,
            OpenedAt = DateTimeOffset.UtcNow
        };

        _state.Account.OpenTrades.Add(trade);
        _state.Account.MarginUsed += fillPrice * request.Units * 0.02m;
        _state.Account.MarginAvailable = _state.Account.Equity - _state.Account.MarginUsed;

        return Task.FromResult(new OrderResult
        {
            OrderId = Guid.NewGuid().ToString("N"),
            BrokerTradeId = trade.TradeId,
            State = OrderState.Filled,
            BrokerMessage = "Paper fill",
            FillPrice = fillPrice,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public Task<IReadOnlyList<OpenTrade>> GetOpenTradesAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<OpenTrade>>(_state.Account.OpenTrades.ToList());

    public Task CloseTradeAsync(string tradeId, CancellationToken ct)
    {
        _state.Account.OpenTrades.RemoveAll(x => x.TradeId == tradeId);
        return Task.CompletedTask;
    }

    public Task EngageKillSwitchCloseAllAsync(CancellationToken ct)
    {
        _state.Account.OpenTrades.Clear();
        _state.Account.MarginUsed = 0m;
        _state.Account.MarginAvailable = _state.Account.Equity;
        return Task.CompletedTask;
    }
}
