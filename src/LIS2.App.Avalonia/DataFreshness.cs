namespace LIS2.App.Avalonia;

internal sealed record FreshnessInfo(string State, DateTimeOffset? LastValidAt, double? AgeSeconds);

/// <summary>Gültige Empfangszeitpunkte behalten; bloße UI-Abfragen verjüngen keine Messung.</summary>
internal sealed class DataFreshness
{
    internal static bool IsValidSample(object? value) => !LIS2.Display.TemplateRenderer.IsMissingValue(value) &&
        (value is not double number || double.IsFinite(number)) &&
        (value is not float single || float.IsFinite(single));
    private readonly Dictionary<string, DateTimeOffset> _lastValid = new(StringComparer.OrdinalIgnoreCase);
    internal void Reset(string key) => _lastValid.Remove(key);
    internal void Observe(string key, DateTimeOffset? receivedAt, bool valid)
    {
        if (valid && receivedAt is { } timestamp && (!_lastValid.TryGetValue(key, out var old) || timestamp > old))
            _lastValid[key] = timestamp;
    }

    internal FreshnessInfo Read(string key, DateTimeOffset now, bool connected, TimeSpan? staleAfter = null)
    {
        if (!_lastValid.TryGetValue(key, out var timestamp)) return new("No data received", null, null);
        var age = Math.Max(0, (now - timestamp).TotalSeconds);
        return new(!connected ? "Disconnected" : staleAfter is { } limit && age >= limit.TotalSeconds ? "Stale" : "Current",
            timestamp, age);
    }
}
