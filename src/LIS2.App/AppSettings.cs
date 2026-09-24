namespace LIS2.App;

public sealed class AppSettings
{
    public string TransportMode { get; set; } = "Virtual";
    public string? PortName { get; set; }
    public int BrightnessPercent { get; set; } = 100;
    public string ThemeMode { get; set; } = nameof(AppThemeMode.System);
    public string LanguageMode { get; set; } = nameof(AppLanguageMode.System);
    public List<PageDefinition> Pages { get; set; } = CreateDefaultPages();
    public List<CustomGlyphSettings> CustomGlyphs { get; set; } = CreateDefaultGlyphs();
    public bool ProgramCustomGlyphsOnConnect { get; set; }
    public List<HardwareSensorPreferenceSettings> HardwareSensorPreferences { get; set; } = new();
    public FanSettings Fans { get; set; } = new();

    public void EnsureDefaults()
    {
        if (Pages.Count == 0)
            Pages = CreateDefaultPages();

        if (CustomGlyphs is null || CustomGlyphs.Count != 8)
            CustomGlyphs = CreateDefaultGlyphs();

        foreach (var glyph in CustomGlyphs)
            glyph.EnsureDefaults();

        HardwareSensorPreferences ??= new List<HardwareSensorPreferenceSettings>();
        HardwareSensorPreferences = HardwareSensorPreferences
            .Where(preference => !string.IsNullOrWhiteSpace(preference.Key))
            .GroupBy(preference => preference.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToList();

        Fans ??= new FanSettings();
        Fans.EnsureDefaults();
    }

    private static List<CustomGlyphSettings> CreateDefaultGlyphs() =>
        Enumerable.Range(1, 8)
            .Select(slot => new CustomGlyphSettings { Name = $"Glyph {slot}" })
            .ToList();

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
            },
            new PageDefinition
            {
                Id = "winamp",
                Name = "Winamp",
                Line1Template = "{Winamp.Artist}",
                Line2Template = "{Winamp.Title}",
                DurationSeconds = 5,
                VisibilityExpression = "Winamp.State=Playing"
            }
        };
}
