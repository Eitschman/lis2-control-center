using LIS2.Runtime;
using LIS2.Sources;

namespace LIS2.App.Avalonia;

/// <summary>Portable template variable adapter over the shared source runtime.</summary>
public sealed class PageSourceValues : IAsyncDisposable
{
    private readonly SourceRuntime _runtime = new();
    private readonly Dictionary<string, object?> _hardware = new(StringComparer.OrdinalIgnoreCase);

    public void UpdateHardware(IReadOnlyDictionary<string, object?> snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _hardware.Clear();
        foreach (var pair in snapshot)
            if (pair.Key.StartsWith("Hardware.", StringComparison.OrdinalIgnoreCase))
                _hardware[pair.Key] = pair.Value;
    }

    public HomeAssistantDataSource HomeAssistant => _runtime.HomeAssistant;
    public IReadOnlyDictionary<string, string?> Errors => _runtime.Errors;

    public Task ConfigureHomeAssistantAsync(string? url, string? token) =>
        _runtime.ConfigureHomeAssistantAsync(url, token);

    public IReadOnlyDictionary<string, object?> Snapshot(DateTimeOffset now)
    {
        var values = new Dictionary<string, object?>(_runtime.Snapshot(), StringComparer.OrdinalIgnoreCase);
        foreach (var pair in _hardware) values[pair.Key] = pair.Value;
        foreach (var pair in PageClockValues.Create(now))
            values.TryAdd(pair.Key, pair.Value);
        for (var slot = 1; slot <= 8; slot++)
            values[$"Glyph.{slot}"] = LIS2.Core.Lis2Protocol.CustomGlyph(slot).ToString();
        return values;
    }

    public Task StartAsync(CancellationToken cancellationToken = default) =>
        _runtime.StartAsync(cancellationToken);

    public ValueTask DisposeAsync() => _runtime.DisposeAsync();
}
