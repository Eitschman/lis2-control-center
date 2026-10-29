using LIS2.App.Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;

namespace LIS2.App.Avalonia.Tests;

public sealed class PagePresetCatalogTests
{
    [AvaloniaTheory]
    [InlineData(true, false, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 13, 14 })]
    [InlineData(false, true, new[] { 0, 1, 2, 3, 4, 5, 6, 11, 15, 16 })]
    public void PlatformPickerContainsOnlySupportedPresetsWithStableIds(bool isWindows, bool isLinux, int[] expected)
    {
        var picker = new ComboBox();
        for (var index = 0; index < 17; index++)
            picker.Items.Add(new ComboBoxItem { Content = "Preset " + index });
        MainWindow.FilterPagePresets(picker, isWindows, isLinux);
        Assert.Equal(expected, picker.Items.OfType<ComboBoxItem>().Select(item => (int)item.Tag!));
        Assert.All(picker.Items.OfType<ComboBoxItem>(), item =>
        {
            Assert.True(item.IsVisible);
            Assert.True(item.IsEnabled);
        });
        Assert.Equal(0, picker.SelectedIndex);
        var last = picker.Items.OfType<ComboBoxItem>().Last();
        picker.SelectedItem = last;
        Assert.Equal(expected.Last(), last.Tag);
    }

    [Theory]
    [InlineData(0, "Clock", "{Clock.Time}", "{Clock.Date}", 5, "", "Truncate", "Truncate")]
    [InlineData(1, "LIS2 Status", "LIS2 Control Center", "Ready", 5, "", "Truncate", "Truncate")]
    [InlineData(7, "Winamp Now Playing", "{Winamp.Artist}", "{Winamp.Title}", 7, "Winamp.State=Playing", "PingPong", "PingPong")]
    [InlineData(8, "Winamp VU", "{Winamp.Title}", "{Winamp.Vu}", 7, "Winamp.State=Playing", "PingPong", "Truncate")]
    [InlineData(9, "Winamp Spectrum", "{Winamp.Artist}", "{Winamp.SpectrumGlyphs}", 7, "Winamp.State=Playing", "PingPong", "Truncate")]
    [InlineData(10, "Windows Media Now Playing", "{WindowsMedia.Artist|fallback:--}", "{WindowsMedia.Title|fallback:--}", 7, "WindowsMedia.State=Playing", "PingPong", "PingPong")]
    [InlineData(11, "Linux media", "{Media.Title}", "{Media.Artist}", 7, "Media.State=Playing", "PingPong", "PingPong")]
    [InlineData(15, "Linux MPRIS VU", "{Media.Title}", "{Media.Vu}", 7, "Media.AudioConnected=True", "PingPong", "Truncate")]
    [InlineData(16, "Linux MPRIS Spectrum", "{Media.Artist}", "{Media.SpectrumGlyphs}", 7, "Media.AudioConnected=True", "PingPong", "Truncate")]
    [InlineData(12, "WMP Legacy Now Playing", "{WmpLegacy.Artist}", "{WmpLegacy.Title}", 7, "WmpLegacy.State=Playing", "PingPong", "PingPong")]
    [InlineData(13, "WMP Legacy VU", "{WmpLegacy.Title}", "{WmpLegacy.Vu}", 7, "WmpLegacy.State=Playing", "PingPong", "Truncate")]
    [InlineData(14, "WMP Legacy Spectrum", "{WmpLegacy.Artist}", "{WmpLegacy.SpectrumGlyphs}", 7, "WmpLegacy.State=Playing", "PingPong", "Truncate")]
    public void StaticPresetsKeepTheirSourceAndVisibility(int index, string name, string line1, string line2,
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
