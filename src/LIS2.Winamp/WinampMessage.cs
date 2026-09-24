namespace LIS2.Winamp;

public sealed class WinampMessage
{
    public string Type { get; set; } = "snapshot";
    public string? State { get; set; }
    public string? Artist { get; set; }
    public string? Title { get; set; }
    public string? Album { get; set; }
    public int? PlaylistPosition { get; set; }
    public int? PlaylistCount { get; set; }
    public double? ElapsedSeconds { get; set; }
    public double? DurationSeconds { get; set; }
    public int? BitrateKbps { get; set; }
    public int? SampleRateHz { get; set; }
    public int? VuLeft { get; set; }
    public int? VuRight { get; set; }
    public int[]? Spectrum { get; set; }
}
