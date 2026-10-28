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
        var keys = new HashSet<string>(current.Where(pair => IsSensorKey(pair.Key) && IsNumeric(pair.Value))
            .Select(pair => pair.Key), StringComparer.OrdinalIgnoreCase);
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
                 entry.Alias.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                 entry.HardwareName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                 entry.SensorName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                 entry.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                 entry.SensorType.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(entry => entry.IsFavorite)
            .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string BuildSummary(IReadOnlyDictionary<string, object?> snapshot,
        IReadOnlyCollection<HardwareSensorEntry> visible, string? sourceError = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(visible);
        if (!string.IsNullOrWhiteSpace(sourceError))
            return "Hardware source error: " + sourceError;
        var total = snapshot.Count(pair => IsSensorKey(pair.Key));
        var unavailable = visible.Count(entry => !entry.IsAvailable);
        return unavailable > 0
            ? $"{visible.Count} visible sensor(s) / {total} total values · {unavailable} unavailable configured sensor(s)"
            : $"{visible.Count} visible sensor(s) / {total} total values";
    }

    public static bool IsSensorKey(string key) =>
        key.StartsWith("Hardware.", StringComparison.OrdinalIgnoreCase) &&
        !key.EndsWith(".Unit", StringComparison.OrdinalIgnoreCase);

    private static bool IsNumeric(object? value) =>
        value is not null && double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
            NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number);

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
                // Template format specifiers are not part of the sensor key.
                // Keep the original key for unavailable-sensor tracking.
                var formatSeparator = token.IndexOf(':');
                if (formatSeparator >= 0) token = token[..formatSeparator].Trim();
                if (IsSensorKey(token)) keys.Add(token);
                offset = close + 1;
            }
        }
        return keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static bool MatchesType(HardwareSensorEntry entry, string? typeFilter)
    {
        if (string.IsNullOrWhiteSpace(typeFilter) ||
            string.Equals(typeFilter, "All types", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(typeFilter, "All", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(typeFilter, "Favorites", StringComparison.OrdinalIgnoreCase)) return entry.IsFavorite;
        if (string.Equals(typeFilter, "Unavailable", StringComparison.OrdinalIgnoreCase)) return !entry.IsAvailable;
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
    public string HardwareName => Key.Split('.').Length > 1 ? Humanize(Key.Split('.')[1]) : "Hardware";
    public string SensorName => Key.Split('.').Length > 3 ? Humanize(string.Join(" ", Key.Split('.').Skip(3))) : Key;

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var result = new System.Text.StringBuilder();
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (character is '_' or '-') { if (result.Length > 0 && result[^1] != ' ') result.Append(' '); continue; }
            if (index > 0 && char.IsUpper(character) && result.Length > 0 && result[^1] != ' ' &&
                (char.IsLower(value[index - 1]) ||
                 (char.IsUpper(value[index - 1]) && index + 1 < value.Length && char.IsLower(value[index + 1]))))
                result.Append(' ');
            result.Append(character);
        }
        return result.ToString();
    }
    public string SelectionDetails => DisplayName + " — " + DisplayValue +
        Environment.NewLine + Key;
    public string Markers => (IsFavorite ? "★" : "") + (!IsAvailable ? " !" : "");
    public string SensorType => HardwareSensorCatalog.GetSensorType(Key);
    internal string Language { get; init; } = "en";
    public string DisplaySensorType => UiTranslations.Translate(SensorType, Language);
    public string DisplayValue => IsAvailable
        ? LIS2.Display.SourceValueFormatter.FormatNumeric(Value) +
          (string.IsNullOrWhiteSpace(Unit) ? "" : " " + Unit.Trim())
        : UiTranslations.Translate("Unavailable", Language);

    public static IReadOnlyList<object> GroupForDisplay(IEnumerable<HardwareSensorEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var result = new List<object>();
        foreach (var group in entries.GroupBy(entry => entry.HardwareName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            result.Add(new HardwareSensorGroupHeader(group.Key));
            result.AddRange(group.OrderByDescending(entry => entry.IsFavorite)
                .ThenByDescending(entry => entry.IsAvailable)
                .ThenBy(entry => entry.SensorType, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
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
