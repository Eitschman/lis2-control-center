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
    public HomeAssistantSettings HomeAssistant { get; set; } = new();
    public FanSettings Fans { get; set; } = new();

    public void EnsureDefaults()
    {
        TransportMode = string.Equals(TransportMode, "Serial", StringComparison.OrdinalIgnoreCase)
            ? "Serial"
            : "Virtual";
        BrightnessPercent = BrightnessPercent is 25 or 50 or 75 or 100
            ? BrightnessPercent
            : 100;

        if (!Enum.TryParse<AppThemeMode>(ThemeMode, true, out _))
            ThemeMode = nameof(AppThemeMode.System);

        if (!Enum.TryParse<AppLanguageMode>(LanguageMode, true, out _))
            LanguageMode = nameof(AppLanguageMode.System);

        Pages ??= new List<PageDefinition>();
        Pages = Pages.Where(page => page is not null).ToList();
        if (Pages.Count == 0)
            Pages = CreateDefaultPages();

        foreach (var page in Pages)
        {
            page.Id = string.IsNullOrWhiteSpace(page.Id) ? Guid.NewGuid().ToString("N") : page.Id;
            page.Name = string.IsNullOrWhiteSpace(page.Name) ? "Page" : page.Name;
            page.Line1Template ??= string.Empty;
            page.Line2Template ??= string.Empty;
            page.DurationSeconds = Math.Clamp(page.DurationSeconds, 1, 3600);
            page.Line1OverflowMode = NormalizeOverflowMode(page.Line1OverflowMode);
            page.Line2OverflowMode = NormalizeOverflowMode(page.Line2OverflowMode);
            page.ScrollStepMilliseconds = Math.Clamp(page.ScrollStepMilliseconds, 50, 10000);
            page.ScrollEdgePauseMilliseconds = Math.Clamp(page.ScrollEdgePauseMilliseconds, 0, 60000);
        }

        CustomGlyphs ??= new List<CustomGlyphSettings>();
        if (CustomGlyphs.Count != 8)
        {
            var defaults = CreateDefaultGlyphs();
            for (var index = 0; index < Math.Min(CustomGlyphs.Count, defaults.Count); index++)
                defaults[index] = CustomGlyphs[index] ?? defaults[index];
            CustomGlyphs = defaults;
        }

        foreach (var glyph in CustomGlyphs)
            glyph.EnsureDefaults();

        HardwareSensorPreferences ??= new List<HardwareSensorPreferenceSettings>();
        HardwareSensorPreferences = HardwareSensorPreferences
            .Where(preference => preference is not null && !string.IsNullOrWhiteSpace(preference.Key))
            .GroupBy(preference => preference.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToList();

        HomeAssistant ??= new HomeAssistantSettings();
        HomeAssistant.EnsureDefaults();

        Fans ??= new FanSettings();
        Fans.EnsureDefaults();
    }

    private static string NormalizeOverflowMode(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "truncate" => "Truncate",
            "marquee" => "Marquee",
            _ => "PingPong"
        };

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