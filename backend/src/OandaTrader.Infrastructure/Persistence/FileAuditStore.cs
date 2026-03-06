using System.Text.Json;
using OandaTrader.Application;

namespace OandaTrader.Infrastructure.Persistence;

public sealed class FileAuditStore : IAuditStore
{
    private readonly string _path = Path.Combine(AppContext.BaseDirectory, "audit.log");

    public async Task AppendAsync(string eventType, object payload, CancellationToken ct)
    {
        var line = JsonSerializer.Serialize(new { ts = DateTimeOffset.UtcNow, eventType, payload });
        await File.AppendAllTextAsync(_path, line + Environment.NewLine, ct);
    }

    public async Task<IReadOnlyList<string>> GetRecentAsync(int count, CancellationToken ct)
    {
        if (!File.Exists(_path)) return Array.Empty<string>();
        var lines = await File.ReadAllLinesAsync(_path, ct);
        return lines.TakeLast(count).ToArray();
    }
}