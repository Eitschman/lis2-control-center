using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class PagePresetCatalogTests
{
    [Theory]
    [InlineData(0, "Clock", "{Clock.Time}", "{Clock.Date}", 5, "", "Truncate", "Truncate")]
    [InlineData(1, "LIS2 Status", "LIS2 Control Center", "Ready", 5, "", "Truncate", "Truncate")]
    [InlineData(7, "Winamp Now Playing", "{Winamp.Artist}", "{Winamp.Title}", 7, "Winamp.State=Playing", "PingPong", "PingPong")]
    [InlineData(8, "Winamp VU", "{Winamp.Title}", "{Winamp.Vu}", 7, "Winamp.State=Playing", "PingPong", "Truncate")]
    [InlineData(9, "Winamp Spectrum", "{Winamp.Artist}", "{Winamp.SpectrumGlyphs}", 7, "Winamp.State=Playing", "PingPong", "Truncate")]
    [InlineData(10, "Windows Media Now Playing", "{WindowsMedia.Artist|fallback:--}", "{WindowsMedia.Title|fallback:--}", 7, "WindowsMedia.State=Playing", "PingPong", "PingPong")]
    public void StaticPresetsMatchWpf(int index, string name, string line1, string line2,
        int duration, string visibility, string overflow1, string overflow2)
    {
        var preset = PagePresetCatalog.Get(index);
        Assert.Equal(name, preset.Name);
        Assert.Equal(line1, preset.Line1);
        Assert.Equal(line2, preset.Line2);
        Assert.Equal(duration, preset.Duration);
        Assert.Equal(visibility, preset.Visibility);
        Assert.Equal(overflow1, preset.Overflow1);
        Assert.Equal(overflow2, preset.Overflow2);
        Assert.Equal(250, preset.ScrollStep);
        Assert.Equal(800, preset.EdgePause);
    }

    [Fact]
    public void HardwarePresetsCannotBeMistakenForStaticPresets()
    {
        foreach (var index in new[] { 2, 3, 4, 5, 6 })
            Assert.Throws<ArgumentOutOfRangeException>(() => PagePresetCatalog.Get(index));
    }
}
