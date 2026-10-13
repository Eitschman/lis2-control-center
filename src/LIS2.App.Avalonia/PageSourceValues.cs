using LIS2.Runtime;
using LIS2.Sources;

namespace LIS2.App.Avalonia;

/// <summary>Portable template variable adapter over the shared source runtime.</summary>
public sealed class PageSourceValues : IAsyncDisposable
{
    private readonly SourceRuntime _runtime = new();

    public HomeAssistantDataSource HomeAssistant => _runtime.HomeAssistant;
    public IReadOnlyDictionary<string, string?> Errors => _runtime.Errors;

    public Task ConfigureHomeAssistantAsync(string? url, string? token) =>
        _runtime.ConfigureHomeAssistantAsync(url, token);

    public IReadOnlyDictionary<string, object?> Snapshot(DateTimeOffset now)
    {
        var values = new Dictionary<string, object?>(_runtime.Snapshot(), StringComparer.OrdinalIgnoreCase);
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
