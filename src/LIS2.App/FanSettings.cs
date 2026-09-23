namespace LIS2.App;

public sealed class FanSettings
{
    public FanChannelSettings[] Channels { get; set; } =
    {
        new() { Name = "Fan 1" },
        new() { Name = "Fan 2" },
        new() { Name = "Fan 3" },
        new() { Name = "Fan 4" }
    };
}
