namespace LIS2.WmpLegacy;

public sealed class WmpLegacyVisualizationMessage
{
    public string Type { get; set; } = "visualization";
    public int? VuLeft { get; set; }
    public int? VuRight { get; set; }
    public int[]? Spectrum { get; set; }
}
