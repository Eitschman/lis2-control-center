namespace LIS2.App;

public sealed class FanSettings
{
    public bool AutomaticControlEnabled { get; set; }

    public FanChannelSettings[] Channels { get; set; } =
        CreateDefaultChannels();

    public void EnsureDefaults()
    {
        if (Channels.Length == 4)
            return;

        var defaults = CreateDefaultChannels();

        for (var index = 0; index < Math.Min(Channels.Length, defaults.Length); index++)
            defaults[index] = Channels[index];

        Channels = defaults;
    }

    private static FanChannelSettings[] CreateDefaultChannels() =>
    [
        new() { Name = "Fan 1" },
        new() { Name = "Fan 2" },
        new() { Name = "Fan 3" },
        new() { Name = "Fan 4" }
    ];
}
