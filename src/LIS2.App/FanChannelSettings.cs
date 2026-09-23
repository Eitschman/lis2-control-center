namespace LIS2.App;

public sealed class FanChannelSettings
{
    public string Name { get; set; } = "Fan";
    public string Mode { get; set; } = "Fixed";
    public int FixedPercent { get; set; } = 100;
    public int MinimumPercent { get; set; } = 30;
    public int MaximumPercent { get; set; } = 100;
    public int FailSafePercent { get; set; } = 100;
    public bool AllowStop { get; set; }
}
