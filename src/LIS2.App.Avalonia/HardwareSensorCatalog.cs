using System.Globalization;
namespace LIS2.App.Avalonia;
public static class HardwareSensorCatalog
{
    public static IReadOnlyList<HardwareSensorEntry> Build(IReadOnlyDictionary<string, object?> snapshot,
        IEnumerable<PortableHardwareSensorPreference> preferences, string? search = null, bool favoritesOnly = false)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(preferences);
        var saved = preferences.Where(p => !string.IsNullOrWhiteSpace(p.Key))
            .GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);
        var keys = new HashSet<string>(snapshot.Keys, StringComparer.OrdinalIgnoreCase);
        keys.UnionWith(saved.Keys);
        var filter = search?.Trim() ?? "";
        return keys.Select(key =>
            {
                saved.TryGetValue(key, out var preference);
                var available = snapshot.TryGetValue(key, out var value);
                if (!available)
                {
                    var match = snapshot.FirstOrDefault(pair => string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase));
                    if (match.Key is not null) { available = true; value = match.Value; }
                }
                return new HardwareSensorEntry(key, available ? value : null,
                    preference?.Alias ?? "", preference?.IsFavorite == true, available);
            })
            .Where(entry => (!favoritesOnly || entry.IsFavorite) &&
                (entry.Key.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                 entry.Alias.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(entry => entry.IsFavorite)
            .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
public sealed record HardwareSensorEntry(string Key, object? Value, string Alias, bool IsFavorite, bool IsAvailable)
{
    public override string ToString()
    {
        var name = string.IsNullOrWhiteSpace(Alias) ? Key : Alias + " (" + Key + ")";
        return (IsFavorite ? "★ " : "") + name + "  =  " +
            (IsAvailable ? Convert.ToString(Value, CultureInfo.InvariantCulture) + " °C" : "Unavailable");
    }
}
