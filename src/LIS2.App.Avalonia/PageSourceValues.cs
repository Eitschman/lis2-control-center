using LIS2.Runtime;
using LIS2.Sources;

namespace LIS2.App.Avalonia;

/// <summary>Portable template variable adapter over the shared source runtime.</summary>
public sealed class PageSourceValues : IAsyncDisposable
{
    private readonly SourceRuntime _runtime = new();
    private readonly Dictionary<string, object?> _hardware = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object?> _media = new(StringComparer.OrdinalIgnoreCase);
    private KeyValuePair<string, string>[] _haAliases = [];

    public void SetHomeAssistantAliases(IEnumerable<KeyValuePair<string, string>> aliases) =>
        _haAliases = aliases.ToArray();

    public static string NormalizeHaAlias(string? alias) =>
        new string((alias ?? "").Trim().Select(character => char.IsLetterOrDigit(character) || character is '_' or '-' ? character : '_').ToArray())
            .Trim('_').Replace("__", "_", StringComparison.Ordinal);

    public void UpdateMedia(string sourceId, IReadOnlyDictionary<string, object?> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentNullException.ThrowIfNull(values);
        var prefix = sourceId + ".";
        foreach (var key in _media.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray())
            _media.Remove(key);
        // Disconnected providers may retain their last metadata snapshot. Keep
        // the connection flag, but never expose stale titles or artists to pages.
        if (values.TryGetValue("Connected", out var connected) &&
            connected is bool isConnected && !isConnected)
        {
            _media[prefix + "Connected"] = false;
            return;
        }
        foreach (var pair in values)
            _media[prefix + pair.Key] = pair.Value;
    }

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
        // Originalschlüssel bleiben erhalten; Aliase erhalten auch Einheit und Attribute.
        foreach (var preference in _haAliases)
        {
            var alias = NormalizeHaAlias(preference.Value);
            var original = "HA." + preference.Key;
            if (alias.Length == 0 || !values.ContainsKey(original)) continue;
            foreach (var pair in values.Where(pair => pair.Key.Equals(original, StringComparison.OrdinalIgnoreCase) ||
                pair.Key.StartsWith(original + ".", StringComparison.OrdinalIgnoreCase)).ToArray())
                values.TryAdd("HA." + alias + pair.Key[original.Length..], pair.Value);
        }
        foreach (var pair in _hardware) values[pair.Key] = pair.Value;
        foreach (var pair in _media) values[pair.Key] = pair.Value;
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
