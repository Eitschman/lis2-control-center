namespace LIS2.WindowsMedia;

public static class WindowsMediaValues
{
    public static IReadOnlyDictionary<string, object?> FromSnapshot(WindowsMediaSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["SourceAppUserModelId"] = snapshot.SourceAppUserModelId,
            ["State"] = snapshot.State.ToString(),
            ["Artist"] = snapshot.Artist,
            ["Title"] = snapshot.Title,
            ["Album"] = snapshot.Album,
            ["AlbumArtist"] = snapshot.AlbumArtist,
            ["TrackNumber"] = snapshot.TrackNumber,
            ["AlbumTrackCount"] = snapshot.AlbumTrackCount,
            ["Elapsed"] = snapshot.Position?.ToString(@"mm\:ss"),
            ["Duration"] = snapshot.Duration?.ToString(@"mm\:ss"),
            ["PlaybackRate"] = snapshot.PlaybackRate
        };
    }
}
