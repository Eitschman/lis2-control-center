using LIS2.Sources;

namespace LIS2.App.Avalonia;

/// <summary>Combines original registered source values with portable display-specific glyph variables.</summary>
public sealed class PageSourceValues : IAsyncDisposable
{
    private readonly DataSourceRegistry _registry = new();
    private bool _started;

    public PageSourceValues()
    {
        _registry.Add(new ClockDataSource());
    }

    public IReadOnlyDictionary<string, object?> Snapshot(DateTimeOffset now)
    {
        var values = new Dictionary<string, object?>(_registry.Snapshot(), StringComparer.OrdinalIgnoreCase);
        // Keep the original Clock.Time/Clock.Date/Clock.Day names and values authoritative.
        foreach (var pair in PageClockValues.Create(now))
            values.TryAdd(pair.Key, pair.Value);
        for (var slot = 1; slot <= 8; slot++)
            values[$"Glyph.{slot}"] = LIS2.Core.Lis2Protocol.CustomGlyph(slot).ToString();
        return values;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started) return;
        await _registry.StartAllAsync(cancellationToken);
        _started = true;
    }

    public async ValueTask DisposeAsync()
    {
        await _registry.StopAllAsync();
        await _registry.DisposeAsync();
    }
}
