using LIS2.App.Avalonia;
using LIS2.Display;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageSourceValuesTests
{
    [Fact]
    public async Task LegacySpectrumUsesByteLevelsAndDisconnectedMetadataDisappears()
    {
        await using var sources = new PageSourceValues();
        sources.SetGlyphNames([], spectrumSet: true);
        sources.UpdateMedia("WmpLegacy", new Dictionary<string, object?>
        {
            ["Connected"] = true, ["Title"] = "Legacy track",
            ["SpectrumRaw"] = new[] { 0, 15, 128, 255 }
        });
        var values = sources.Snapshot(DateTimeOffset.Now);
        Assert.Equal("Legacy track", new TemplateRenderer().Render("{WmpLegacy.Title}", values));
        var glyphs = Assert.IsType<string>(values["WmpLegacy.SpectrumGlyphs"]);
        Assert.Equal(' ', glyphs[0]);
        Assert.Equal(LIS2.Core.Lis2Protocol.CustomGlyph(1), glyphs[1]);
        Assert.Equal(LIS2.Core.Lis2Protocol.CustomGlyph(5), glyphs[2]);
        Assert.Equal(LIS2.Core.Lis2Protocol.CustomGlyph(8), glyphs[3]);
        sources.UpdateMedia("WmpLegacy", new Dictionary<string, object?>
        {
            ["Connected"] = true, ["SpectrumRaw"] = Enumerable.Repeat(15, 20).ToArray()
        });
        Assert.Equal(new string(LIS2.Core.Lis2Protocol.CustomGlyph(1), 20),
            sources.Snapshot(DateTimeOffset.Now)["WmpLegacy.SpectrumGlyphs"]);
        sources.UpdateMedia("WmpLegacy", new Dictionary<string, object?>
        {
            ["Connected"] = false, ["VisualizationConnected"] = true, ["Title"] = "Stale title",
            ["SpectrumRaw"] = Enumerable.Repeat(255, 20).ToArray(), ["VuLeft"] = 192
        });
        values = sources.Snapshot(DateTimeOffset.Now);
        Assert.False(values.ContainsKey("WmpLegacy.Title"));
        Assert.Equal(192, values["WmpLegacy.VuLeft"]);
        Assert.Equal(new string(LIS2.Core.Lis2Protocol.CustomGlyph(8), 20), values["WmpLegacy.SpectrumGlyphs"]);
        sources.UpdateMedia("WmpLegacy", new Dictionary<string, object?> { ["Connected"] = false, ["Title"] = "Stale title" });
        values = sources.Snapshot(DateTimeOffset.Now);
        Assert.False(values.ContainsKey("WmpLegacy.Title"));
        Assert.Equal("", values["WmpLegacy.SpectrumGlyphs"]);
    }

    [Fact]
    public async Task OriginalClockKeysAreAvailableAfterStart()
    {
        await using var sources = new PageSourceValues();
        await sources.StartAsync();
        var values = sources.Snapshot(DateTimeOffset.Now);
        Assert.True(values.ContainsKey("Clock.Time"));
        Assert.True(values.ContainsKey("Clock.Date"));
        Assert.True(values.ContainsKey("Clock.Day"));
        Assert.True(values.ContainsKey("Clock.Hour"));
        Assert.True(values.ContainsKey("Glyph.1"));
        var rendered = new TemplateRenderer().Render("{Clock.Time}", values);
        Assert.False(string.IsNullOrWhiteSpace(rendered));
    }

    [Fact]
    public async Task MediaValuesAreNamespacedReplacedAndRenderInPages()
    {
        await using var sources = new PageSourceValues();
        sources.UpdateMedia("Winamp", new Dictionary<string, object?>
        {
            ["Title"] = "First track", ["Artist"] = "Artist A"
        });
        var first = sources.Snapshot(DateTimeOffset.Now);
        Assert.Equal("First track", first["Winamp.Title"]);
        Assert.Equal("First track", new TemplateRenderer().Render("{Winamp.Title}", first));
        sources.UpdateMedia("Winamp", new Dictionary<string, object?> { ["Title"] = "Second track" });
        var second = sources.Snapshot(DateTimeOffset.Now);
        Assert.Equal("Second track", second["Winamp.Title"]);
        Assert.False(second.ContainsKey("Winamp.Artist"));
    }

    [Fact]
    public void LinuxMprisMapsPlaybackMetadata()
    {
        var values = LinuxMprisSource.Map("Playing", "Song", "Artist");
        Assert.Equal(true, values["Connected"]);
        Assert.Equal("Song", values["Title"]);
        Assert.Equal("Artist", values["Artist"]);
        Assert.Equal(false, LinuxMprisSource.Map("", "", "")["Connected"]);
    }

    [Fact]
    public void LinuxMprisParsesAlbumDurationAndMissingData()
    {
        var values = LinuxMprisSource.Parse("<media><state>Playing</state><title>Song</title><artist>Artist</artist><album>Album</album><elapsed>1000000</elapsed><duration>123000000</duration></media>");
        Assert.Equal("Album", values["Album"]);
        Assert.Equal("00:02:03", values["Duration"]);
        Assert.Equal("Song", values["Title"]);
        Assert.Equal("00:00:01", values["Elapsed"]);
        Assert.Throws<System.Xml.XmlException>(() => LinuxMprisSource.Parse("bad response"));
    }

    [Fact]
    public async Task WindowsMediaBridgeHasSafeDisconnectedState()
    {
        await using var bridge = new WindowsMediaBridge();
        Assert.Equal(false, bridge.Values["Connected"]);
        if (!OperatingSystem.IsWindows())
        {
            await bridge.StartAsync();
            Assert.Equal(false, bridge.Values["Connected"]);
        }
    }

    [Fact]
    public async Task SourceLifecycleIsIdempotent()
    {
        await using var sources = new PageSourceValues();
        await sources.StartAsync();
        await sources.StartAsync();
        Assert.NotEmpty(sources.Snapshot(DateTimeOffset.Now));
    }

    [Fact]
    public async Task FailedMediaProviderClearsOnlyItsOwnTemplateNamespace()
    {
        await using var sources = new PageSourceValues();
        sources.UpdateMedia("Winamp", new Dictionary<string, object?>
        {
            ["Connected"] = true, ["Title"] = "Old song"
        });
        sources.UpdateMedia("Media", new Dictionary<string, object?>
        {
            ["Connected"] = true, ["Title"] = "Other player"
        });
        sources.UpdateMedia("Winamp", new Dictionary<string, object?>());
        var snapshot = sources.Snapshot(DateTimeOffset.UnixEpoch);
        Assert.False(snapshot.ContainsKey("Winamp.Title"));
        Assert.False(snapshot.ContainsKey("Winamp.Connected"));
        Assert.Equal("Other player", snapshot["Media.Title"]);
    }

    [Fact]
    public async Task MediaPageTemplateReflectsFreshProviderValues()
    {
        await using var sources = new PageSourceValues();
        var workspace = new PageWorkspace();
        var page = workspace.Add();
        page.Line1Template = "{Media.Title}";
        page.Line2Template = "{Media.Artist}";
        sources.UpdateMedia("Media", new Dictionary<string, object?>
        {
            ["Title"] = "Track A", ["Artist"] = "Artist A"
        });
        Assert.Equal("Track A".PadRight(20),
            workspace.Preview(0, sources.Snapshot(DateTimeOffset.UnixEpoch)).Line1);
        sources.UpdateMedia("Media", new Dictionary<string, object?>
        {
            ["Title"] = "Track B", ["Artist"] = "Artist B"
        });
        var frame = workspace.Preview(0, sources.Snapshot(DateTimeOffset.UnixEpoch));
        Assert.Equal("Track B".PadRight(20), frame.Line1);
        Assert.Equal("Artist B".PadRight(20), frame.Line2);
    }

    [Fact]
    public async Task DisconnectedMediaSnapshotCannotKeepOldSongVisible()
    {
        await using var sources = new PageSourceValues();
        sources.UpdateMedia("Winamp", new Dictionary<string, object?>
        {
            ["Connected"] = true, ["Title"] = "Playing"
        });
        sources.UpdateMedia("Winamp", new Dictionary<string, object?>
        {
            ["Connected"] = false, ["Title"] = "Stale cached song", ["Artist"] = "Stale artist"
        });
        var snapshot = sources.Snapshot(DateTimeOffset.UnixEpoch);
        Assert.Equal(false, snapshot["Winamp.Connected"]);
        Assert.False(snapshot.ContainsKey("Winamp.Title"));
        Assert.False(snapshot.ContainsKey("Winamp.Artist"));
        sources.UpdateMedia("Winamp", new Dictionary<string, object?>
        {
            ["Connected"] = true, ["Title"] = "Fresh song"
        });
        Assert.Equal("Fresh song", sources.Snapshot(DateTimeOffset.UnixEpoch)["Winamp.Title"]);
    }

    [Fact]
    public async Task DisconnectedMediaProviderDoesNotEraseOtherProvider()
    {
        await using var sources = new PageSourceValues();
        sources.UpdateMedia("Media", new Dictionary<string, object?>
        {
            ["Connected"] = true, ["Title"] = "Linux track"
        });
        sources.UpdateMedia("WindowsMedia", new Dictionary<string, object?>
        {
            ["Connected"] = false, ["Title"] = "Old Windows track"
        });
        var snapshot = sources.Snapshot(DateTimeOffset.UnixEpoch);
        Assert.Equal("Linux track", snapshot["Media.Title"]);
        Assert.False(snapshot.ContainsKey("WindowsMedia.Title"));
        Assert.Equal(false, snapshot["WindowsMedia.Connected"]);
    }
}
