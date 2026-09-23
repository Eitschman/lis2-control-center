namespace LIS2.Winamp;

public static class WinampValues
{
    public static IReadOnlyDictionary<string, object?> FromSnapshot(WinampSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["State"] = snapshot.State.ToString(),
            ["Artist"] = snapshot.Artist,
            ["Title"] = snapshot.Title,
            ["Album"] = snapshot.Album,
            ["PlaylistPosition"] = snapshot.PlaylistPosition,
            ["PlaylistCount"] = snapshot.PlaylistCount,
            ["Elapsed"] = snapshot.Elapsed?.ToString(@"mm\:ss"),
            ["Duration"] = snapshot.Duration?.ToString(@"mm\:ss"),
            ["BitrateKbps"] = snapshot.BitrateKbps,
            ["SampleRateHz"] = snapshot.SampleRateHz
        };
    }
}
