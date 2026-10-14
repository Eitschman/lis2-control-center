using System.Globalization;
namespace LIS2.App.Avalonia;
public static class HardwareSensorCatalog
{
    public static IReadOnlyList<HardwareSensorEntry> Build(IReadOnlyDictionary<string, object?> snapshot,
        IEnumerable<PortableHardwareSensorPreference> preferences, string? search = null, bool favoritesOnly = false, string? typeFilter = null, IEnumerable<string>? trackedKeys = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(preferences);
        var saved = preferences.Where(p => !string.IsNullOrWhiteSpace(p.Key))
            .GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);
        var current = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in snapshot) current[pair.Key] = pair.Value;
        var keys = new HashSet<string>(current.Keys.Where(IsSensorKey), StringComparer.OrdinalIgnoreCase);
        keys.UnionWith(saved.Keys.Where(IsSensorKey));
        if (trackedKeys is not null) keys.UnionWith(trackedKeys.Where(IsSensorKey));
        var filter = search?.Trim() ?? "";
        return keys.Select(key =>
            {
                saved.TryGetValue(key, out var preference);
                var available = current.TryGetValue(key, out var value) && IsNumeric(value);
                current.TryGetValue(key + ".Unit", out var unitValue);
                var unit = unitValue as string ?? "";
                return new HardwareSensorEntry(key, available ? value : null,
                    preference?.Alias ?? "", preference?.IsFavorite == true, available, unit);
            })
            .Where(entry => MatchesType(entry, typeFilter) && (!favoritesOnly || entry.IsFavorite) &&
                (entry.Key.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                 entry.Alias.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(entry => entry.IsFavorite)
            .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool IsSensorKey(string key) =>
        key.StartsWith("Hardware.", StringComparison.OrdinalIgnoreCase) &&
        !key.EndsWith(".Unit", StringComparison.OrdinalIgnoreCase);

    private static bool IsNumeric(object? value) =>
        value is not null && double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
            NumberStyles.Float, CultureInfo.InvariantCulture, out _);

    public static IReadOnlyList<string> ExtractTrackedKeys(IEnumerable<string?> templates,
        IEnumerable<string?> fanSensorKeys)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in fanSensorKeys)
            if (!string.IsNullOrWhiteSpace(key) && IsSensorKey(key)) keys.Add(key);
        foreach (var template in templates)
        {
            if (string.IsNullOrEmpty(template)) continue;
            var offset = 0;
            while (offset < template.Length)
            {
                var open = template.IndexOf('{', offset);
                if (open < 0) break;
                var close = template.IndexOf('}', open + 1);
                if (close < 0) break;
                var token = template.Substring(open + 1, close - open - 1).Trim();
                if (IsSensorKey(token)) keys.Add(token);
                offset = close + 1;
            }
        }
        return keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToArray();
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
public sealed record HardwareSensorEntry(string Key, object? Value, string Alias, bool IsFavorite, bool IsAvailable, string Unit = "")
{
    public string DisplayName => string.IsNullOrWhiteSpace(Alias) ? SensorName : Alias;
    public string HardwareName => Key.Split('.').Length > 1 ? Key.Split('.')[1] : "Hardware";
    public string SensorName => Key.Split('.').Length > 3 ? string.Join(" ", Key.Split('.').Skip(3)) : Key;
    public string Markers => (IsFavorite ? "★" : "") + (!IsAvailable ? " !" : "");
    public string SensorType => HardwareSensorCatalog.GetSensorType(Key);
    public string DisplayValue => IsAvailable
        ? (Convert.ToString(Value, CultureInfo.InvariantCulture) ?? "") +
          (string.IsNullOrWhiteSpace(Unit) ? "" : " " + Unit.Trim())
        : "Unavailable";

    public static IReadOnlyList<object> GroupForDisplay(IEnumerable<HardwareSensorEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var result = new List<object>();
        foreach (var group in entries.GroupBy(entry => entry.HardwareName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            result.Add(new HardwareSensorGroupHeader(group.Key));
            result.AddRange(group.OrderByDescending(entry => entry.IsFavorite)
                .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase));
        }
        return result;
    }

    public override string ToString()
    {
        var name = string.IsNullOrWhiteSpace(Alias) ? Key : Alias + " (" + Key + ")";
        return (IsFavorite ? "★ " : "") + name + "  =  " +
            DisplayValue;
    }
}

public sealed record HardwareSensorGroupHeader(string Name);
