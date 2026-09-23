namespace LIS2.Winamp;

public sealed record WinampSnapshot(
    WinampPlaybackState State,
    string? Artist,
    string? Title,
    string? Album,
    int? PlaylistPosition,
    int? PlaylistCount,
    TimeSpan? Elapsed,
    TimeSpan? Duration,
    int? BitrateKbps,
    int? SampleRateHz);
