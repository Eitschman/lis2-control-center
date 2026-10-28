using LIS2.WindowsMedia;

namespace LIS2.WindowsMedia.Tests;

public sealed class WindowsMediaValuesTests
{
    [Theory]
    [InlineData("de-DE", "1,25")]
    [InlineData("en-US", "1.25")]
    public void PlaybackRateUsesRegionalNumbersWhileTitlesAndTimesStayIntact(string culture, string expected)
    {
        var original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(culture);
            var snapshot = new WindowsMediaSnapshot("Microsoft.ZuneMusic", WindowsMediaPlaybackState.Playing,
                "1.25", "21.5", null, null, 2, 12, TimeSpan.FromSeconds(65), TimeSpan.FromSeconds(125), 1.25);
            var values = WindowsMediaValues.FromSnapshot(snapshot).ToDictionary(pair => "WindowsMedia." + pair.Key, pair => pair.Value);
            Assert.Equal(expected + " / 21.5 / 1.25 / 01:05", new LIS2.Display.TemplateRenderer().Render(
                "{WindowsMedia.PlaybackRate} / {WindowsMedia.Title} / {WindowsMedia.Artist} / {WindowsMedia.Elapsed}", values));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = original; }
    }

    [Theory]
    [InlineData("Björk", "Björk")]
    [InlineData(null, "Unveränderter Untertitel")]
    public void SubtitleIsAnExplicitReplacementAndDoesNotOverwriteArtist(string? artist, string expected)
    {
        var snapshot = new WindowsMediaSnapshot("Microsoft.ZuneMusic", WindowsMediaPlaybackState.Playing,
            artist, "Titel", null, null, null, null, null, null, null, "Unveränderter Untertitel");
        var mapped = WindowsMediaValues.FromSnapshot(snapshot);
        Assert.Equal(artist, mapped["Artist"]);
        Assert.Equal("Unveränderter Untertitel", mapped["Subtitle"]);
        Assert.DoesNotContain("StreamName", mapped.Keys);
        var values = mapped.ToDictionary(pair => "WindowsMedia." + pair.Key, pair => pair.Value);
        Assert.Equal(expected, new LIS2.Display.TemplateRenderer().Render(
            "{WindowsMedia.Artist|fallbackvar:WindowsMedia.Subtitle|fallback:--}", values));
        Assert.Null(WindowsMediaValues.FromSnapshot(snapshot with { Subtitle = null })["Subtitle"]);
    }

    [Fact]
    public void Snapshot_ExposesCommonDisplayValues()
    {
        var snapshot = new WindowsMediaSnapshot(
            "Microsoft.ZuneMusic_8wekyb3d8bbwe!Microsoft.ZuneMusic",
            WindowsMediaPlaybackState.Playing,
            "Dire Straits",
            "Telegraph Road",
            "Love over Gold",
            "Dire Straits",
            1,
            5,
            TimeSpan.FromSeconds(65),
            TimeSpan.FromSeconds(845),
            1.0);

        var values = WindowsMediaValues.FromSnapshot(snapshot);

        Assert.Equal("Playing", values["State"]);
        Assert.Equal("Dire Straits", values["Artist"]);
        Assert.Equal("Telegraph Road", values["Title"]);
        Assert.Equal("Love over Gold", values["Album"]);
        Assert.Equal("Dire Straits", values["AlbumArtist"]);
        Assert.Equal(1, values["TrackNumber"]);
        Assert.Equal(5, values["AlbumTrackCount"]);
        Assert.Equal("01:05", values["Elapsed"]);
        Assert.Equal("14:05", values["Duration"]);
        Assert.Equal(1.0, values["PlaybackRate"]);
    }
}
