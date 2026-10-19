using LIS2.App.Avalonia;
using LIS2.Display;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageSourceValuesTests
{
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
        var values = LinuxMprisSource.Parse("Playing\tSong\tArtist\tAlbum\t123000000");
        Assert.Equal("Album", values["Album"]);
        Assert.Equal("00:02:03", values["Duration"]);
        Assert.Equal("Song", values["Title"]);
        Assert.Equal(false, LinuxMprisSource.Parse("bad response")["Connected"]);
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
}
