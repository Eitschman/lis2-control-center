namespace LIS2.App;

public sealed class FanChannelSettings
{
    public string Name { get; set; } = "Fan";
    public string Mode { get; set; } = "Fixed";
    public string? SensorKey { get; set; }
    public int FixedPercent { get; set; } = 100;
    public int MinimumPercent { get; set; } = 30;
    public int MaximumPercent { get; set; } = 100;
    public int FailSafePercent { get; set; } = 100;
    public bool AllowStop { get; set; }
    public List<FanCurvePointSettings> Curve { get; set; } =
        new()
        {
            new FanCurvePointSettings { Temperature = 40, OutputPercent = 40 },
            new FanCurvePointSettings { Temperature = 60, OutputPercent = 80 },
            new FanCurvePointSettings { Temperature = 75, OutputPercent = 100 }
        };
}
