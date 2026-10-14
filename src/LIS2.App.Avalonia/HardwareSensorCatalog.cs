using System.Globalization;
namespace LIS2.App.Avalonia;
public static class HardwareSensorCatalog
{
    public static IReadOnlyList<HardwareSensorEntry> Build(IReadOnlyDictionary<string, object?> snapshot,
        IEnumerable<PortableHardwareSensorPreference> preferences, string? search = null, bool favoritesOnly = false, string? typeFilter = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(preferences);
        var saved = preferences.Where(p => !string.IsNullOrWhiteSpace(p.Key))
            .GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);
        var current = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in snapshot) current[pair.Key] = pair.Value;
        var keys = new HashSet<string>(current.Keys, StringComparer.OrdinalIgnoreCase);
        keys.UnionWith(saved.Keys);
        var filter = search?.Trim() ?? "";
        return keys.Select(key =>
            {
                saved.TryGetValue(key, out var preference);
                var available = current.TryGetValue(key, out var value);
                return new HardwareSensorEntry(key, available ? value : null,
                    preference?.Alias ?? "", preference?.IsFavorite == true, available);
            })
            .Where(entry => MatchesType(entry, typeFilter) && (!favoritesOnly || entry.IsFavorite) &&
                (entry.Key.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                 entry.Alias.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(entry => entry.IsFavorite)
            .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool MatchesType(HardwareSensorEntry entry, string? typeFilter)
    {
        if (string.IsNullOrWhiteSpace(typeFilter) || typeFilter == "All types") return true;
        if (typeFilter == "Favorites") return entry.IsFavorite;
        if (typeFilter == "Unavailable") return !entry.IsAvailable;
        return string.Equals(typeFilter, GetSensorType(entry.Key), StringComparison.OrdinalIgnoreCase);
    }

    public static string GetSensorType(string key)
    {
        var types = new[] { "Temperature", "Load", "Fan", "Clock", "Voltage", "Power", "Data", "Throughput" };
        return types.FirstOrDefault(type => key.Split('.', '/', ':')
            .Any(part => string.Equals(part, type, StringComparison.OrdinalIgnoreCase))) ?? "Other";
    }
}
public sealed record HardwareSensorEntry(string Key, object? Value, string Alias, bool IsFavorite, bool IsAvailable)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Alias) ? Key : Alias;
    public string Markers => (IsFavorite ? "★" : "") + (!IsAvailable ? " !" : "");
    public string SensorType => HardwareSensorCatalog.GetSensorType(Key);
    public string DisplayValue => IsAvailable ? Convert.ToString(Value, CultureInfo.InvariantCulture) ?? "" : "Unavailable";

    public override string ToString()
    {
        var name = string.IsNullOrWhiteSpace(Alias) ? Key : Alias + " (" + Key + ")";
        return (IsFavorite ? "★ " : "") + name + "  =  " +
            (IsAvailable ? Convert.ToString(Value, CultureInfo.InvariantCulture) + " °C" : "Unavailable");
    }
}
