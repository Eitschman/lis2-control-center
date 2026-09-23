namespace LIS2.Fans;

public sealed class FanChannelConfiguration
{
    public FanMode Mode { get; set; } = FanMode.Fixed;
    public string? SensorKey { get; set; }
    public int FixedPercent { get; set; } = 100;
    public int MinimumPercent { get; set; } = 30;
    public int MaximumPercent { get; set; } = 100;
    public int FailSafePercent { get; set; } = 100;
    public bool AllowStop { get; set; }
    public List<FanCurvePoint> Curve { get; set; } = new();
}
