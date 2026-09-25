namespace LIS2.WmpLegacy;

public static class WmpLegacyValues
{
    public static IReadOnlyDictionary<string, object?> FromSnapshot(WmpLegacySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["State"] = snapshot.State.ToString(),
            ["Artist"] = snapshot.Artist,
            ["Title"] = snapshot.Title,
            ["Album"] = snapshot.Album,
            ["TrackNumber"] = snapshot.TrackNumber,
            ["PlaylistCount"] = snapshot.PlaylistCount,
            ["Elapsed"] = snapshot.Elapsed?.ToString(@"mm\:ss"),
            ["Duration"] = snapshot.Duration?.ToString(@"mm\:ss")
        };
    }
}
