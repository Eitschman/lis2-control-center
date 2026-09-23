namespace LIS2.App;

public sealed class AppSettings
{
    public string TransportMode { get; set; } = "Virtual";
    public string? PortName { get; set; }
    public int BrightnessPercent { get; set; } = 100;
    public List<PageDefinition> Pages { get; set; } = CreateDefaultPages();
    public FanSettings Fans { get; set; } = new();

    public void EnsureDefaults()
    {
        if (Pages.Count == 0)
            Pages = CreateDefaultPages();
    }

    private static List<PageDefinition> CreateDefaultPages() =>
        new()
        {
            new PageDefinition
            {
                Id = "clock",
                Name = "Clock",
                Line1Template = "{Clock.Time}",
                Line2Template = "{Clock.Date}",
                DurationSeconds = 5
            },
            new PageDefinition
            {
                Id = "status",
                Name = "Status",
                Line1Template = "LIS2 Control Center",
                Line2Template = "Virtual/Serial ready",
                DurationSeconds = 5
            }
        };
}
