namespace LIS2.App;

public sealed class FanChannelSettings
{
    public string Name { get; set; } = "Fan";

    public string DisplayName
    {
        get
        {
            for (var index = 1; index <= 4; index++)
            {
                if (string.Equals(
                        Name,
                        $"Fan {index}",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return LocalizationService.Format("Fan {0}", index);
                }
            }

            return Name;
        }
    }

    public string Mode { get; set; } = "Fixed";
    public string? SensorKey { get; set; }
    public int FixedPercent { get; set; } = 100;
    public int MinimumPercent { get; set; } = 30;
    public int MaximumPercent { get; set; } = 100;
    public int FailSafePercent { get; set; } = 100;
    public double HysteresisDegrees { get; set; } = 1.0;
    public bool AllowStop { get; set; }
    public List<FanCurvePointSettings> Curve { get; set; } =
        new()
        {
            new FanCurvePointSettings { Temperature = 40, OutputPercent = 40 },
            new FanCurvePointSettings { Temperature = 60, OutputPercent = 80 },
            new FanCurvePointSettings { Temperature = 75, OutputPercent = 100 }
        };

    public void EnsureDefaults()
    {
        Name = string.IsNullOrWhiteSpace(Name) ? "Fan" : Name;

        Mode = Mode?.Trim().ToLowerInvariant() switch
        {
            "curve" => "Curve",
            "follow" => "Follow",
            "external" => "External",
            "off" => "Off",
            _ => "Fixed"
        };

        MinimumPercent = Math.Clamp(MinimumPercent, 0, 100);
        MaximumPercent = Math.Clamp(MaximumPercent, MinimumPercent, 100);
        FixedPercent = Math.Clamp(FixedPercent, 0, 100);
        FailSafePercent = Math.Clamp(FailSafePercent, MinimumPercent, MaximumPercent);

        if (double.IsNaN(HysteresisDegrees) || double.IsInfinity(HysteresisDegrees))
            HysteresisDegrees = 1.0;
        HysteresisDegrees = Math.Clamp(HysteresisDegrees, 0, 50);

        Curve ??= new List<FanCurvePointSettings>();
        Curve = Curve
            .Where(point => point is not null &&
                            !double.IsNaN(point.Temperature) &&
                            !double.IsInfinity(point.Temperature))
            .Select(point =>
            {
                point.OutputPercent = Math.Clamp(point.OutputPercent, 0, 100);
                return point;
            })
            .OrderBy(point => point.Temperature)
            .ToList();
    }
}