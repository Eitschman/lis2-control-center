namespace LIS2.App.Avalonia;

/// <summary>Freie Dashboard-Werte und ausdrücklich zugeordnete Drehzahlquellen ohne Hardwarefreigabe.</summary>
public sealed class DashboardSettings
{
    public const int ValueCount = 8;
    public List<DashboardValueSetting> Values { get; set; } = [];
    public List<string> FanSpeedKeys { get; set; } = [];

    public static DashboardSettings Normalize(DashboardSettings? source) => new()
    {
        Values = Enumerable.Range(0, ValueCount).Select(index => new DashboardValueSetting
        {
            Label = source?.Values?.ElementAtOrDefault(index)?.Label?.Trim() ?? "",
            Template = source?.Values?.ElementAtOrDefault(index)?.Template ?? ""
        }).ToList(),
        FanSpeedKeys = Enumerable.Range(0, 4)
            .Select(index => source?.FanSpeedKeys?.ElementAtOrDefault(index)?.Trim() ?? "").ToList()
    };
}

public sealed class DashboardValueSetting
{
    public string Label { get; set; } = "";
    public string Template { get; set; } = "";
}
