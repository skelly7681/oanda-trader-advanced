using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OandaTrader.Application;
using OandaTrader.Domain;
using OandaTrader.Infrastructure.Configuration;

namespace OandaTrader.Infrastructure.Brokers;

public sealed class OandaBrokerGateway : IBrokerGateway
{
    private readonly HttpClient _httpClient;
    private readonly OandaOptions _options;

    public TradingMode Mode => _options.Environment.Equals("live", StringComparison.OrdinalIgnoreCase)
        ? TradingMode.Live
        : TradingMode.Practice;

    public OandaBrokerGateway(HttpClient httpClient, OandaOptions options)
    {
        _httpClient = httpClient;
        _options = options;

        var baseUrl = Mode == TradingMode.Live ? _options.LiveRestBaseUrl : _options.PracticeRestBaseUrl;
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<AccountSnapshot> GetAccountSnapshotAsync(CancellationToken ct)
    {
        var res = await _httpClient.GetAsync($"/v3/accounts/{_options.AccountId}/summary", ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(json);
        var account = doc.RootElement.GetProperty("account");

        return new AccountSnapshot
        {
            AccountId = _options.AccountId,
            Balance = decimal.Parse(account.GetProperty("balance").GetString() ?? "0"),
            Equity = decimal.Parse(account.GetProperty("NAV").GetString() ?? "0"),
            MarginUsed = decimal.Parse(account.GetProperty("marginUsed").GetString() ?? "0"),
            MarginAvailable = decimal.Parse(account.GetProperty("marginAvailable").GetString() ?? "0"),
            UnrealizedPnL = decimal.Parse(account.GetProperty("unrealizedPL").GetString() ?? "0"),
            StartingBalance = decimal.Parse(account.GetProperty("balance").GetString() ?? "0"),
            DailyPnL = 0m,
            OpenTrades = new(),
            Positions = new()
        };
    }

    public async Task<IReadOnlyList<PriceTick>> GetLatestPricesAsync(IEnumerable<string> instruments, CancellationToken ct)
    {
        var query = string.Join(",", instruments);
        var res = await _httpClient.GetAsync($"/v3/accounts/{_options.AccountId}/pricing?instruments={query}", ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var list = new List<PriceTick>();
        foreach (var p in doc.RootElement.GetProperty("prices").EnumerateArray())
        {
            var instrument = p.GetProperty("instrument").GetString()!;
            var bid = decimal.Parse(p.GetProperty("bids")[0].GetProperty("price").GetString() ?? "0");
            var ask = decimal.Parse(p.GetProperty("asks")[0].GetProperty("price").GetString() ?? "0");
            var time = DateTimeOffset.Parse(p.GetProperty("time").GetString()!);
            list.Add(new PriceTick(instrument, bid, ask, time));
        }

        return list;
    }

    public async Task<OrderResult> PlaceOrderAsync(OrderRequest request, CancellationToken ct)
    {
        var units = request.Side == TradeSide.Buy ? request.Units : -request.Units;

        var payload = new
        {
            order = new
            {
                units = units.ToString(System.Globalization.CultureInfo.InvariantCulture),
                instrument = request.Instrument,
                timeInForce = "FOK",
                type = "MARKET",
                positionFill = "DEFAULT",
                stopLossOnFill = request.StopLoss is null ? null : new { price = request.StopLoss.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                takeProfitOnFill = request.TakeProfit is null ? null : new { price = request.TakeProfit.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) }
            }
        };

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/v3/accounts/{_options.AccountId}/orders");
        msg.Headers.Add("ClientRequestID", request.ClientOrderId);
        msg.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var res = await _httpClient.SendAsync(msg, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
        {
            return new OrderResult
            {
                OrderId = request.ClientOrderId,
                State = OrderState.Rejected,
                BrokerMessage = body,
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        return new OrderResult
        {
            OrderId = request.ClientOrderId,
            State = OrderState.Filled,
            BrokerMessage = body,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public async Task<IReadOnlyList<OpenTrade>> GetOpenTradesAsync(CancellationToken ct)
    {
        var res = await _httpClient.GetAsync($"/v3/accounts/{_options.AccountId}/openTrades", ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var list = new List<OpenTrade>();
        foreach (var t in doc.RootElement.GetProperty("trades").EnumerateArray())
        {
            var units = decimal.Parse(t.GetProperty("currentUnits").GetString() ?? "0");
            list.Add(new OpenTrade
            {
                TradeId = t.GetProperty("id").GetString()!,
                Instrument = t.GetProperty("instrument").GetString()!,
                Side = units >= 0 ? TradeSide.Buy : TradeSide.Sell,
                Units = Math.Abs(units),
                EntryPrice = decimal.Parse(t.GetProperty("price").GetString() ?? "0"),
                UnrealizedPnL = decimal.Parse(t.GetProperty("unrealizedPL").GetString() ?? "0"),
                OpenedAt = DateTimeOffset.Parse(t.GetProperty("openTime").GetString()!),
                StopLoss = null,
                TakeProfit = null
            });
        }
        return list;
    }

    public async Task CloseTradeAsync(string tradeId, CancellationToken ct)
    {
        var res = await _httpClient.PutAsync($"/v3/accounts/{_options.AccountId}/trades/{tradeId}/close", content: null, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task EngageKillSwitchCloseAllAsync(CancellationToken ct)
    {
        var trades = await GetOpenTradesAsync(ct);
        foreach (var trade in trades)
            await CloseTradeAsync(trade.TradeId, ct);
    }
}