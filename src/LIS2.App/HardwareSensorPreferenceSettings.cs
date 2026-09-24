namespace LIS2.App;

public sealed class HardwareSensorPreferenceSettings
{
    public string Key { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
}
