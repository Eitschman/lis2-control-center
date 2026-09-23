using LIS2.Winamp;

namespace LIS2.Winamp.Tests;

public sealed class WinampMappingTests
{
    [Fact]
    public void PipeMessage_MapsToSnapshot()
    {
        var message = new WinampMessage
        {
            State = "playing",
            Artist = "Iron Maiden",
            Title = "The Trooper",
            ElapsedSeconds = 65,
            DurationSeconds = 240,
            BitrateKbps = 320
        };

        var snapshot = WinampPipeServer.ToSnapshot(message);

        Assert.Equal(WinampPlaybackState.Playing, snapshot.State);
        Assert.Equal("Iron Maiden", snapshot.Artist);
        Assert.Equal("The Trooper", snapshot.Title);
        Assert.Equal(TimeSpan.FromSeconds(65), snapshot.Elapsed);
        Assert.Equal(TimeSpan.FromSeconds(240), snapshot.Duration);
        Assert.Equal(320, snapshot.BitrateKbps);
    }

    [Fact]
    public void Snapshot_ExposesDisplayValues()
    {
        var snapshot = new WinampSnapshot(
            WinampPlaybackState.Playing,
            "Artist",
            "Title",
            "Album",
            3,
            10,
            TimeSpan.FromSeconds(65),
            TimeSpan.FromSeconds(245),
            320,
            44100);

        var values = WinampValues.FromSnapshot(snapshot);

        Assert.Equal("Playing", values["State"]);
        Assert.Equal("Artist", values["Artist"]);
        Assert.Equal("01:05", values["Elapsed"]);
        Assert.Equal("04:05", values["Duration"]);
    }
}
