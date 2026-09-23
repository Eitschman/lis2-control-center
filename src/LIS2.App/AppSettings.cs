namespace LIS2.App;

public sealed class AppSettings
{
    public string TransportMode { get; set; } = "Virtual";
    public string? PortName { get; set; }
    public int BrightnessPercent { get; set; } = 100;
}
