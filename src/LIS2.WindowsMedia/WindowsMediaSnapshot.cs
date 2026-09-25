namespace LIS2.WindowsMedia;

public sealed record WindowsMediaSnapshot(
    string SourceAppUserModelId,
    WindowsMediaPlaybackState State,
    string? Artist,
    string? Title,
    string? Album,
    string? AlbumArtist,
    int? TrackNumber,
    int? AlbumTrackCount,
    TimeSpan? Position,
    TimeSpan? Duration,
    double? PlaybackRate);
