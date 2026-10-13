using LIS2.Fans;

namespace LIS2.App.Avalonia;

/// <summary>Portable WPF-compatible fan settings. Persistence never enables physical output.</summary>
public sealed class PortableFanSettings
{
    public bool AutomaticControlEnabled { get; set; }
    public List<PortableFanChannel> Channels { get; set; } = Defaults();

    private static List<PortableFanChannel> Defaults() =>
        Enumerable.Range(1, 4).Select(i => new PortableFanChannel { Name = $"Fan {i}" }).ToList();

    public static PortableFanSettings Normalize(PortableFanSettings? source)
    {
        var channels = Defaults();
        if (source?.Channels is { } imported)
            for (var i = 0; i < Math.Min(4, imported.Count); i++)
                if (imported[i] is not null) channels[i] = PortableFanChannel.Normalize(imported[i], i + 1);
        return new PortableFanSettings { AutomaticControlEnabled = source?.AutomaticControlEnabled == true, Channels = channels };
    }

    public FanChannelConfiguration[] ToRuntimeChannels() =>
        Normalize(this).Channels.Select(x => x.ToRuntime()).ToArray();
}

public sealed class PortableFanChannel
{
    public string Name { get; set; } = "Fan";
    public string Mode { get; set; } = "Fixed";
    public string? SensorKey { get; set; }
    public int FixedPercent { get; set; } = 100;
    public int MinimumPercent { get; set; } = 30;
    public int MaximumPercent { get; set; } = 100;
    public int FailSafePercent { get; set; } = 100;
    public double HysteresisDegrees { get; set; } = 1;
    public bool AllowStop { get; set; }
    public List<PortableFanCurvePoint> Curve { get; set; } = new()
    {
        new(40, 40), new(60, 80), new(75, 100)
    };

    public static PortableFanChannel Normalize(PortableFanChannel source, int number)
    {
        var min = Math.Clamp(source.MinimumPercent, 0, 100);
        var max = Math.Clamp(source.MaximumPercent, min, 100);
        return new PortableFanChannel
        {
            Name = string.IsNullOrWhiteSpace(source.Name) ? $"Fan {number}" : source.Name,
            Mode = Enum.TryParse<FanMode>(source.Mode, true, out var mode) && Enum.IsDefined(mode) ? mode.ToString() : "Fixed",
            SensorKey = source.SensorKey, FixedPercent = Math.Clamp(source.FixedPercent, 0, 100),
            MinimumPercent = min, MaximumPercent = max,
            FailSafePercent = Math.Clamp(source.FailSafePercent, min, max),
            HysteresisDegrees = double.IsFinite(source.HysteresisDegrees) ? Math.Clamp(source.HysteresisDegrees, 0, 50) : 1,
            AllowStop = source.AllowStop,
            Curve = (source.Curve ?? new()).Where(p => p is not null && double.IsFinite(p.Temperature))
                .Select(p => new PortableFanCurvePoint(p.Temperature, Math.Clamp(p.OutputPercent, 0, 100)))
                .OrderBy(p => p.Temperature).ToList()
        };
    }

    public FanChannelConfiguration ToRuntime() => new()
    {
        Mode = Enum.Parse<FanMode>(Mode), SensorKey = SensorKey,
        FixedPercent = FixedPercent, MinimumPercent = MinimumPercent, MaximumPercent = MaximumPercent,
        FailSafePercent = FailSafePercent, HysteresisDegrees = HysteresisDegrees, AllowStop = AllowStop,
        Curve = Curve.Select(p => new FanCurvePoint(p.Temperature, p.OutputPercent)).ToList()
    };
}

public sealed record PortableFanCurvePoint(double Temperature, int OutputPercent);
