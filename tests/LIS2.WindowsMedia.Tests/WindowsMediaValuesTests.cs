using LIS2.WindowsMedia;

namespace LIS2.WindowsMedia.Tests;

public sealed class WindowsMediaValuesTests
{
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
