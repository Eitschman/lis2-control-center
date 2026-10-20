namespace LIS2.App.Avalonia;

/// <summary>Searchable, bounded catalog for page template variables; keys remain unchanged.</summary>
public static class PageVariableBrowserCatalog
{
    public static string Category(string key)
    {
        if (key.StartsWith("Hardware.", StringComparison.OrdinalIgnoreCase)) return "Hardware";
        if (key.StartsWith("HomeAssistant.", StringComparison.OrdinalIgnoreCase) ||
            key.StartsWith("HA.", StringComparison.OrdinalIgnoreCase)) return "Home Assistant";
        if (key.StartsWith("Clock.", StringComparison.OrdinalIgnoreCase)) return "Clock";
        return "Media / Other";
    }

    public static string Entity(string key)
    {
        if (Category(key) != "Home Assistant") return "";
        var remainder = key[(key.IndexOf('.') + 1)..];
        var dot = remainder.IndexOf('.');
        if (dot < 0) return remainder;
        // Entity ids use domain.object_id, optionally followed by an attribute.
        var second = remainder.IndexOf('.', dot + 1);
        return second < 0 ? remainder : remainder[..second];
    }

    public static string Label(string key, IReadOnlyDictionary<string, string>? friendlyNames = null)
    {
        var entity = Entity(key);
        if (entity.Length > 0)
        {
            var name = friendlyNames is not null && friendlyNames.TryGetValue(entity, out var friendly)
                ? friendly : entity.Replace('_', ' ');
            var prefix = key[..(key.IndexOf('.') + 1)];
            var suffix = key[(prefix.Length + entity.Length)..];
            return name + (suffix.Length == 0 ? " · State" : " · " + suffix.TrimStart('.').Replace('_', ' '));
        }
        return key.Replace('_', ' ').Replace('.', ' · ');
    }

    public static IReadOnlyList<string> Search(IEnumerable<string> keys, string? query,
        string? category, IReadOnlyCollection<string>? favorites, int limit = 50)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var terms = (query ?? "").Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var favoriteSet = new HashSet<string>(favorites ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        return keys.Where(key => !string.IsNullOrWhiteSpace(key)
                && (string.IsNullOrEmpty(category) || category == "All" ||
                    string.Equals(category, "Favorites", StringComparison.OrdinalIgnoreCase)
                        ? string.IsNullOrEmpty(category) || category == "All" || favoriteSet.Contains(key)
                        : string.Equals(Category(key), category, StringComparison.OrdinalIgnoreCase))
                && terms.All(term => key.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(key => favoriteSet.Contains(key))
            .ThenBy(key => key, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(limit, 1, 500))
            .ToArray();
    }
}
