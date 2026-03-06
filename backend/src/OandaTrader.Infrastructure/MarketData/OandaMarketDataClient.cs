using System.Text.Json;
using OandaTrader.Application;
using OandaTrader.Domain;
using OandaTrader.Infrastructure.Configuration;

namespace OandaTrader.Infrastructure.MarketData;

public sealed class OandaMarketDataClient : IMarketDataClient
{
    private readonly HttpClient _httpClient;
    private readonly OandaOptions _options;

    public OandaMarketDataClient(HttpClient httpClient, OandaOptions options)
    {
        _httpClient = httpClient;
        _options = options;
        var baseUrl = _options.Environment.Equals("live", StringComparison.OrdinalIgnoreCase)
            ? _options.LiveRestBaseUrl
            : _options.PracticeRestBaseUrl;
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<IReadOnlyList<Candle>> GetCandlesAsync(string instrument, string granularity, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var url = $"/v3/instruments/{instrument}/candles?price=M&granularity={granularity}&from={Uri.EscapeDataString(from.UtcDateTime.ToString("O"))}&to={Uri.EscapeDataString(to.UtcDateTime.ToString("O"))}";
        var res = await _httpClient.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var candles = new List<Candle>();
        foreach (var c in doc.RootElement.GetProperty("candles").EnumerateArray())
        {
            var mid = c.GetProperty("mid");
            candles.Add(new Candle(
                instrument,
                granularity,
                DateTimeOffset.Parse(c.GetProperty("time").GetString()!),
                decimal.Parse(mid.GetProperty("o").GetString() ?? "0"),
                decimal.Parse(mid.GetProperty("h").GetString() ?? "0"),
                decimal.Parse(mid.GetProperty("l").GetString() ?? "0"),
                decimal.Parse(mid.GetProperty("c").GetString() ?? "0"),
                c.GetProperty("volume").GetInt64(),
                c.GetProperty("complete").GetBoolean()));
        }
        return candles;
    }
}