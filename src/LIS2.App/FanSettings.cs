namespace LIS2.App;

public sealed class FanSettings
{
    public bool AutomaticControlEnabled { get; set; }

    public FanChannelSettings[] Channels { get; set; } =
        CreateDefaultChannels();

    public void EnsureDefaults()
    {
        Channels ??= Array.Empty<FanChannelSettings>();

        var defaults = CreateDefaultChannels();

        for (var index = 0; index < Math.Min(Channels.Length, defaults.Length); index++)
        {
            if (Channels[index] is not null)
                defaults[index] = Channels[index];
        }

        Channels = defaults;

        foreach (var channel in Channels)
            channel.EnsureDefaults();
    }

    private static FanChannelSettings[] CreateDefaultChannels() =>
    [
        new() { Name = "Fan 1" },
        new() { Name = "Fan 2" },
        new() { Name = "Fan 3" },
        new() { Name = "Fan 4" }
    ];
}