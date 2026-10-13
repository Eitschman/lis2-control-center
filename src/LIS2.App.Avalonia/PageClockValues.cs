using System.Globalization;

namespace LIS2.App.Avalonia;

/// <summary>Cross-platform live clock variables for the existing page template engine.</summary>
public static class PageClockValues
{
    public static IReadOnlyDictionary<string, object?> Create(DateTimeOffset instant)
    {
        var local = instant.ToLocalTime();
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Clock.Time"] = local.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            ["Clock.Date"] = local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["Clock.Hour"] = local.Hour,
            ["Clock.Minute"] = local.Minute,
            ["Clock.Second"] = local.Second,
            ["Clock.Day"] = local.Day,
            ["Clock.Month"] = local.Month,
            ["Clock.Year"] = local.Year,
            ["Clock.DayOfWeek"] = local.DayOfWeek.ToString()
        };
        return result;
    }
}
