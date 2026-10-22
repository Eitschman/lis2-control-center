namespace LIS2.App.Avalonia;

/// <summary>Exact non-hardware page preset definitions from WPF MainWindow.CreatePagePresetDefinition.</summary>
public sealed record PagePresetSpec(
    string Name, string Line1, string Line2, int Duration = 5,
    string Visibility = "", string Overflow1 = "Truncate",
    string Overflow2 = "Truncate", int ScrollStep = 250, int EdgePause = 800);

public static class PagePresetCatalog
{
    public static PagePresetSpec Get(int choice) => choice switch
    {
        0 => new("Clock", "{Clock.Time}", "{Clock.Date}"),
        1 => new("LIS2 Status", "LIS2 Control Center", "Ready"),
        7 => new("Winamp Now Playing", "{Winamp.Artist}", "{Winamp.Title}", 7,
            "Winamp.State=Playing", "PingPong", "PingPong"),
        8 => new("Winamp VU", "{Winamp.Title}", "{Winamp.Vu}", 7,
            "Winamp.State=Playing", "PingPong", "Truncate"),
        9 => new("Winamp Spectrum", "{Winamp.Artist}", "{Winamp.SpectrumGlyphs}", 7,
            "Winamp.State=Playing", "PingPong", "Truncate"),
        10 => new("Windows Media Now Playing", "{WindowsMedia.Artist|fallback:--}",
            "{WindowsMedia.Title|fallback:--}", 7,
            "WindowsMedia.State=Playing", "PingPong", "PingPong"),
        12 => new("WMP Legacy Now Playing", "{WmpLegacy.Artist}", "{WmpLegacy.Title}", 7,
            "WmpLegacy.State=Playing", "PingPong", "PingPong"),
        13 => new("WMP Legacy VU", "{WmpLegacy.Title}", "{WmpLegacy.Vu}", 7,
            "WmpLegacy.State=Playing", "PingPong", "Truncate"),
        14 => new("WMP Legacy Spectrum", "{WmpLegacy.Artist}", "{WmpLegacy.SpectrumGlyphs}", 7,
            "WmpLegacy.State=Playing", "PingPong", "Truncate"),
        _ => throw new ArgumentOutOfRangeException(nameof(choice), "Unknown static page preset.")
    };
}
