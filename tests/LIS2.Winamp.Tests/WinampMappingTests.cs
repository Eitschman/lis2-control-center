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
            BitrateKbps = 320,
            VuLeft = 180,
            VuRight = 160,
            Spectrum = Enumerable.Range(0, 20).Select(index => index * 10).ToArray()
        };

        var snapshot = WinampPipeServer.ToSnapshot(message);

        Assert.Equal(WinampPlaybackState.Playing, snapshot.State);
        Assert.Equal("Iron Maiden", snapshot.Artist);
        Assert.Equal("The Trooper", snapshot.Title);
        Assert.Equal(TimeSpan.FromSeconds(65), snapshot.Elapsed);
        Assert.Equal(TimeSpan.FromSeconds(240), snapshot.Duration);
        Assert.Equal(320, snapshot.BitrateKbps);
        Assert.Equal(180, snapshot.VuLeft);
        Assert.Equal(160, snapshot.VuRight);
        Assert.Equal(20, snapshot.Spectrum?.Count);
    }

    [Fact]
    public void NativePluginCamelCaseJson_DeserializesAllFields()
    {
        const string json =
            """
            {"type":"snapshot","state":"playing","artist":"Iron Maiden","title":"The Trooper","album":"Piece of Mind","playlistPosition":3,"playlistCount":12,"elapsedSeconds":65,"durationSeconds":245,"bitrateKbps":320,"sampleRateHz":44100,"vuLeft":200,"vuRight":180,"spectrum":[0,10,20,30]}
            """;

        var message = WinampPipeServer.DeserializeMessage(json);

        Assert.NotNull(message);
        Assert.Equal("playing", message.State);
        Assert.Equal("Iron Maiden", message.Artist);
        Assert.Equal("The Trooper", message.Title);
        Assert.Equal("Piece of Mind", message.Album);
        Assert.Equal(3, message.PlaylistPosition);
        Assert.Equal(12, message.PlaylistCount);
        Assert.Equal(65, message.ElapsedSeconds);
        Assert.Equal(245, message.DurationSeconds);
        Assert.Equal(320, message.BitrateKbps);
        Assert.Equal(44100, message.SampleRateHz);
        Assert.Equal(200, message.VuLeft);
        Assert.Equal(180, message.VuRight);
        Assert.Equal(new[] { 0, 10, 20, 30 }, message.Spectrum);
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
            44100,
            200,
            180,
            Enumerable.Repeat(128, 20).ToArray());

        var values = WinampValues.FromSnapshot(snapshot);

        Assert.Equal("Playing", values["State"]);
        Assert.Equal("Artist", values["Artist"]);
        Assert.Equal("01:05", values["Elapsed"]);
        Assert.Equal("04:05", values["Duration"]);
        Assert.Equal(200, values["VuLeft"]);
        Assert.Equal(180, values["VuRight"]);
        Assert.Equal(20, Assert.IsType<string>(values["Spectrum"]).Length);
    }

    [Fact]
    public void VuText_IsExactlyTwentyCharacters()
    {
        var text = WinampValues.FormatVu(255, 128);

        Assert.Equal(20, text.Length);
        Assert.StartsWith("L||||||||", text);
    }

    [Fact]
    public async Task DataSource_MarksSnapshotStaleAfterTimeout()
    {
        var time = new ManualTimeProvider(
            new DateTimeOffset(2026, 9, 25, 10, 0, 0, TimeSpan.Zero));
        await using var source = new WinampDataSource(time);

        source.ApplySnapshot(new WinampSnapshot(
            WinampPlaybackState.Playing,
            "Artist",
            "Title",
            "Album",
            1,
            1,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(4),
            320,
            44100,
            100,
            100,
            Enumerable.Repeat(0, 20).ToArray()));

        Assert.True(source.IsRecentlyConnected);
        Assert.Equal(true, source.Values["Connected"]);
        Assert.Equal("Playing", source.Values["State"]);

        time.Advance(TimeSpan.FromSeconds(4));

        Assert.False(source.IsRecentlyConnected);
        Assert.Equal(false, source.Values["Connected"]);
        Assert.Equal("Unknown", source.Values["State"]);
        Assert.Equal("Title", source.Values["Title"]);
    }

    [Fact]
    public void SpectrumFormatter_ScalesClassicZeroToFifteenRange()
    {
        var text = WinampValues.FormatSpectrum(
            new[] { 0, 1, 2, 4, 6, 8, 10, 12, 15, 0, 1, 2, 4, 6, 8, 10, 12, 15, 8, 4 });

        Assert.Equal(20, text.Length);
        Assert.Contains('#', text);
        Assert.NotEqual(new string(' ', 20), text);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ManualTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow += duration;
    }
}
