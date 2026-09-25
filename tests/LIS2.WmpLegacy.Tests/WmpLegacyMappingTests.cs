using LIS2.WmpLegacy;

namespace LIS2.WmpLegacy.Tests;

public sealed class WmpLegacyMappingTests
{
    [Fact]
    public void PipeMessage_MapsToSnapshot()
    {
        var message = new WmpLegacyMessage
        {
            State = "playing",
            Artist = "Dire Straits",
            Title = "Telegraph Road",
            Album = "Love over Gold",
            TrackNumber = 1,
            PlaylistCount = 12,
            ElapsedSeconds = 65,
            DurationSeconds = 845
        };

        var snapshot = WmpLegacyPipeServer.ToSnapshot(message);

        Assert.Equal(WmpLegacyPlaybackState.Playing, snapshot.State);
        Assert.Equal("Dire Straits", snapshot.Artist);
        Assert.Equal("Telegraph Road", snapshot.Title);
        Assert.Equal("Love over Gold", snapshot.Album);
        Assert.Equal(1, snapshot.TrackNumber);
        Assert.Equal(12, snapshot.PlaylistCount);
        Assert.Equal(TimeSpan.FromSeconds(65), snapshot.Elapsed);
        Assert.Equal(TimeSpan.FromSeconds(845), snapshot.Duration);
    }

    [Fact]
    public void NativePluginJson_DeserializesAllFields()
    {
        const string json =
            """
            {"type":"snapshot","state":"paused","artist":"Artist","title":"Title","album":"Album","trackNumber":3,"playlistCount":9,"elapsedSeconds":12.5,"durationSeconds":245.0}
            """;

        var message = WmpLegacyPipeServer.DeserializeMessage(json);

        Assert.NotNull(message);
        Assert.Equal("paused", message.State);
        Assert.Equal("Artist", message.Artist);
        Assert.Equal("Title", message.Title);
        Assert.Equal("Album", message.Album);
        Assert.Equal(3, message.TrackNumber);
        Assert.Equal(9, message.PlaylistCount);
        Assert.Equal(12.5, message.ElapsedSeconds);
        Assert.Equal(245.0, message.DurationSeconds);
    }

    [Fact]
    public void Snapshot_ExposesDisplayValues()
    {
        var snapshot = new WmpLegacySnapshot(
            WmpLegacyPlaybackState.Playing,
            "Artist",
            "Title",
            "Album",
            3,
            9,
            TimeSpan.FromSeconds(65),
            TimeSpan.FromSeconds(245));

        var values = WmpLegacyValues.FromSnapshot(snapshot);

        Assert.Equal("Playing", values["State"]);
        Assert.Equal("Artist", values["Artist"]);
        Assert.Equal("Title", values["Title"]);
        Assert.Equal("Album", values["Album"]);
        Assert.Equal(3, values["TrackNumber"]);
        Assert.Equal(9, values["PlaylistCount"]);
        Assert.Equal("01:05", values["Elapsed"]);
        Assert.Equal("04:05", values["Duration"]);
    }

    [Fact]
    public async Task DataSource_MarksSnapshotStaleAfterTimeout()
    {
        var time = new ManualTimeProvider(
            new DateTimeOffset(2026, 9, 25, 10, 0, 0, TimeSpan.Zero));
        await using var source = new WmpLegacyDataSource(time);

        source.ApplySnapshot(new WmpLegacySnapshot(
            WmpLegacyPlaybackState.Playing,
            "Artist",
            "Title",
            "Album",
            1,
            1,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(4)));

        Assert.True(source.IsRecentlyConnected);
        Assert.Equal(true, source.Values["Connected"]);
        Assert.Equal("Playing", source.Values["State"]);

        time.Advance(TimeSpan.FromSeconds(4));

        Assert.False(source.IsRecentlyConnected);
        Assert.Equal(false, source.Values["Connected"]);
        Assert.Equal("Unknown", source.Values["State"]);
        Assert.Equal("Title", source.Values["Title"]);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ManualTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow += duration;
    }
}
