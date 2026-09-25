namespace LIS2.WmpLegacy;

public sealed class WmpLegacyMessage
{
    public string Type { get; set; } = "snapshot";
    public string? State { get; set; }
    public string? Artist { get; set; }
    public string? Title { get; set; }
    public string? Album { get; set; }
    public int? TrackNumber { get; set; }
    public int? PlaylistCount { get; set; }
    public double? ElapsedSeconds { get; set; }
    public double? DurationSeconds { get; set; }
}
