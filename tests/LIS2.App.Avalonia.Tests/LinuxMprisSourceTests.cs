using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class LinuxMprisSourceTests
{
    [Fact]
    public async Task RealPlayerctlReadsRadioAndFileFromPrivateMprisBus()
    {
        if (!OperatingSystem.IsLinux()) return;
        var info = new System.Diagnostics.ProcessStartInfo("dbus-run-session")
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
        };
        info.ArgumentList.Add("--");
        info.ArgumentList.Add("/usr/bin/python3");
        info.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "TestData", "mpris_service.py"));
        using var fixture = System.Diagnostics.Process.Start(info)!;
        var errors = fixture.StandardError.ReadToEndAsync();
        try
        {
            var ready = await fixture.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(string.IsNullOrEmpty(ready), "Private MPRIS fixture failed: " + (fixture.HasExited ? await errors : "no startup output"));
            using var document = System.Text.Json.JsonDocument.Parse(ready!);
            var address = document.RootElement.GetProperty("address").GetString()!;
            var source = new LinuxMprisSource(command: (arguments, cancellation) =>
                LinuxMprisSource.ExecuteAsync("/usr/bin/env", new[] { "DBUS_SESSION_BUS_ADDRESS=" + address, "playerctl" }.Concat(arguments).ToArray(),
                    cancellation, TimeSpan.FromSeconds(5)));
            foreach (var (player, artist, title) in new[] {
                ("lis2radio", "RADIO BOB - Symphonic Metal", "Eleine - We Are Legion"),
                ("lis2file", "Björk", "Jóga & <Live>"),
                ("lis2empty", "", "Artist - Combined stream title") })
            {
                var snapshot = await source.ReadSnapshotAsync(player);
                Assert.Equal(true, snapshot.Values["Connected"]);
                Assert.Equal(artist, snapshot.Values["Artist"]);
                Assert.Equal(title, snapshot.Values["Title"]);
            }
        }
        finally
        {
            if (!fixture.HasExited) fixture.Kill(entireProcessTree: true);
            await fixture.WaitForExitAsync();
            await errors;
        }
    }

    [Theory]
    [InlineData("RADIO BOB - Symphonic Metal", "Eleine - We Are Legion")]
    [InlineData("Björk", "Jóga")]
    [InlineData("", "Artist - Combined stream title")]
    public async Task StreamAndFileMetadataStayUnmodified(string artist, string title)
    {
        var metadata = new System.Xml.Linq.XElement("media",
            new System.Xml.Linq.XElement("state", "Playing"),
            new System.Xml.Linq.XElement("artist", artist),
            new System.Xml.Linq.XElement("title", title),
            new System.Xml.Linq.XElement("album", ""),
            new System.Xml.Linq.XElement("elapsed", ""),
            new System.Xml.Linq.XElement("duration", "")).ToString();
        var source = new LinuxMprisSource(command: (arguments, _) => Task.FromResult(
            new PlayerctlResult(0, arguments[0] == "--list-all" ? "tuner\n" : metadata, "")));
        var snapshot = await source.ReadSnapshotAsync("tuner");
        Assert.Equal(artist, snapshot.Values["Artist"]);
        Assert.Equal(title, snapshot.Values["Title"]);
        var values = snapshot.Values.ToDictionary(pair => "Media." + pair.Key, pair => pair.Value);
        Assert.Equal(artist.Length == 0 ? "--" : artist, new LIS2.Display.TemplateRenderer().Render(
            "{Media.Artist|fallback:--}", values));
        Assert.DoesNotContain("StreamName", snapshot.Values.Keys);
    }

    internal const string Metadata = "<media><state>Playing</state><title>A | B &amp; &lt;C&gt;\nTitel</title>" +
        "<artist>Connected</artist><album>Album</album><elapsed>65000000</elapsed><duration>90061000000</duration></media>";

    [Fact]
    public async Task ExplicitSelectionKeepsInstanceAndMetadataIntact()
    {
        var calls = new List<IReadOnlyList<string>>();
        var source = new LinuxMprisSource(command: (arguments, _) =>
        {
            calls.Add(arguments);
            return Task.FromResult(arguments[0] == "--list-all" ? new PlayerctlResult(0, "vlc.instance1\nplexamp\n", "") : new(0, Metadata, ""));
        });
        var snapshot = await source.ReadSnapshotAsync("plexamp");
        Assert.Equal("--player=plexamp", calls[1][0]);
        Assert.Equal("plexamp", snapshot.Values["Player"]);
        Assert.Equal("A | B & <C>\nTitel", snapshot.Values["Title"]);
        Assert.Equal("Connected", snapshot.Values["Artist"]);
        Assert.Equal("00:01:05", snapshot.Values["Elapsed"]);
        Assert.Equal("25:01:01", snapshot.Values["Duration"]);
        Assert.DoesNotContain("Vu", snapshot.Values.Keys);
        Assert.DoesNotContain("Spectrum", snapshot.Values.Keys);
    }

    [Fact]
    public async Task PlayerExitDoesNotSelectAnotherPlayerAndReconnectsOnReturn()
    {
        var available = false;
        var metadataReads = 0;
        var source = new LinuxMprisSource(command: (arguments, _) =>
        {
            if (arguments[0] == "--list-all") return Task.FromResult(new PlayerctlResult(0, available ? "vlc\nplexamp\n" : "vlc\n", ""));
            metadataReads++;
            return Task.FromResult(new PlayerctlResult(0, Metadata, ""));
        });
        Assert.Equal(false, (await source.ReadSnapshotAsync("plexamp")).Values["Connected"]);
        Assert.Equal(0, metadataReads);
        available = true;
        Assert.Equal(true, (await source.ReadSnapshotAsync("plexamp")).Values["Connected"]);
        available = false;
        var gone = await source.ReadSnapshotAsync("plexamp");
        Assert.Equal(false, gone.Values["Connected"]);
        Assert.Equal("", gone.Values["Title"]);
        Assert.Equal(1, metadataReads);
    }

    [Fact]
    public async Task AutomaticSelectionUsesFirstListedPlayer()
    {
        var source = new LinuxMprisSource(command: (arguments, _) =>
        {
            if (arguments[0] == "--list-all") return Task.FromResult(new PlayerctlResult(0, "vlc.instance42\nplexamp\n", ""));
            Assert.Equal("--player=vlc.instance42", arguments[0]);
            Assert.Contains("{{position}}", arguments[^1]);
            Assert.Contains("markup_escape", arguments[^1]);
            return Task.FromResult(new PlayerctlResult(0, Metadata, ""));
        });
        Assert.Equal("vlc.instance42", (await source.ReadAsync())["Player"]);
    }

    [Theory]
    [InlineData("No players found")]
    [InlineData("No player could handle this command")]
    public async Task NoPlayersIsDisconnected(string error)
    {
        var source = new LinuxMprisSource(command: (_, _) => Task.FromResult(new PlayerctlResult(1, "", error)));
        Assert.Equal(false, (await source.ReadAsync())["Connected"]);
    }

    [Fact]
    public async Task ProviderFailureIsReportedAndCanRecover()
    {
        var failed = true;
        var source = new LinuxMprisSource(command: (arguments, _) => Task.FromResult(failed ? new PlayerctlResult(1, "", "Cannot connect to session bus") :
            arguments[0] == "--list-all" ? new(0, "vlc\n", "") : new(0, Metadata, "")));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => source.ReadAsync());
        Assert.Contains("Cannot connect to session bus", error.Message);
        failed = false;
        Assert.Equal(true, (await source.ReadAsync())["Connected"]);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("bad")]
    [InlineData("9223372036854775807")]
    public void InvalidTimesAreErrors(string elapsed)
        => Assert.Throws<FormatException>(() => LinuxMprisSource.Parse(Metadata.Replace("65000000", elapsed)));

    [Fact]
    public void MissingTimesRemainUnavailable()
    {
        var values = LinuxMprisSource.Parse(Metadata.Replace("65000000", "").Replace("90061000000", ""));
        Assert.Null(values["Elapsed"]);
        Assert.Null(values["Duration"]);
        Assert.Throws<FormatException>(() => LinuxMprisSource.Parse(Metadata.Replace("Playing", "Invalid")));
    }

    [Fact]
    public async Task CancellationIsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var source = new LinuxMprisSource(command: (_, token) => Task.FromCanceled<PlayerctlResult>(token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.ReadAsync(cancellation.Token));
    }

    [Fact]
    public async Task RealProcessDrainsOutputReportsFailureAndStopsOnTimeoutOrCancellation()
    {
        // Die Prozessverwaltung wird auf Linux gegen echte Kindprozesse geprüft.
        if (!OperatingSystem.IsLinux()) return;
        var valid = await LinuxMprisSource.ExecuteAsync("/bin/sh", ["-c", "printf output; printf error >&2; exit 7"],
            CancellationToken.None, TimeSpan.FromSeconds(3));
        Assert.Equal(new PlayerctlResult(7, "output", "error"), valid);
        await Assert.ThrowsAsync<TimeoutException>(() => LinuxMprisSource.ExecuteAsync("/bin/sh", ["-c", "sleep 60"],
            CancellationToken.None, TimeSpan.FromMilliseconds(100)));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => LinuxMprisSource.ExecuteAsync("/bin/sh", ["-c", "sleep 60"],
            cancellation.Token, TimeSpan.FromSeconds(3)));
    }
}
