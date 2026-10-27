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
    int? SampleRateHz,
    int? VuLeft,
    int? VuRight,
    IReadOnlyList<int>? Spectrum,
    string? StreamName = null,
    string? StreamTitle = null,
    string? StreamGenre = null,
    string? StreamUrl = null,
    bool? IsStream = null,
    string? TitleSource = null);
