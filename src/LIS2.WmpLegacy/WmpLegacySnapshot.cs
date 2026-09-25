namespace LIS2.WmpLegacy;

public sealed record WmpLegacySnapshot(
    WmpLegacyPlaybackState State,
    string? Artist,
    string? Title,
    string? Album,
    int? TrackNumber,
    int? PlaylistCount,
    TimeSpan? Elapsed,
    TimeSpan? Duration);
