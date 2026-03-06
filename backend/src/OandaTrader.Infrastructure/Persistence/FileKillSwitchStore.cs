using OandaTrader.Application;
using OandaTrader.Domain;

namespace OandaTrader.Infrastructure.Persistence;

public sealed class FileKillSwitchStore : IKillSwitchStore
{
    private readonly string _path = Path.Combine(AppContext.BaseDirectory, "kill-switch.txt");

    public async Task<KillSwitchState> GetStateAsync(CancellationToken ct)
    {
        if (!File.Exists(_path)) return KillSwitchState.Disengaged;
        var raw = await File.ReadAllTextAsync(_path, ct);
        return Enum.TryParse<KillSwitchState>(raw, true, out var state) ? state : KillSwitchState.Disengaged;
    }

    public Task SetStateAsync(KillSwitchState state, CancellationToken ct)
        => File.WriteAllTextAsync(_path, state.ToString(), ct);
}