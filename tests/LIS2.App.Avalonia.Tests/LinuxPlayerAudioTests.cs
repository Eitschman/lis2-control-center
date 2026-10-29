using System.Diagnostics;
using System.Text.Json;
using LIS2.App.Avalonia;
using LIS2.WmpLegacy.Audio;

namespace LIS2.App.Avalonia.Tests;

public sealed class LinuxPlayerAudioTests
{
    private const string Graph = """
        [
          {"id":3,"type":"PipeWire:Interface:Client","info":{"props":{"pipewire.sec.pid":42}}},
          {"id":4,"type":"PipeWire:Interface:Node","info":{"state":"running","props":{"object.serial":104,"client.id":3,"node.name":"radio","media.class":"Stream/Output/Audio"}}},
          {"id":5,"type":"PipeWire:Interface:Node","info":{"state":"running","props":{"object.serial":105,"application.process.id":99,"node.name":"other","media.class":"Stream/Output/Audio"}}},
          {"id":6,"type":"PipeWire:Interface:Node","info":{"props":{"object.serial":106,"application.process.id":42,"node.name":"microphone","media.class":"Audio/Source"}}},
          {"id":7,"type":"PipeWire:Interface:Node","info":{"props":{"object.serial":107,"application.process.id":42,"node.name":"speakers","media.class":"Audio/Sink"}}}
        ]
        """;

    [Fact]
    public void SandboxProcessNumbersUseTheSelectedInstanceAndRejectOtherApplications()
    {
        var identity = new LinuxSandboxIdentity("org.example.Player", "123");
        var selection = new LinuxSandboxSelection(identity,
            [new(500, 2, identity), new(501, 3, identity), new(600, 2, new("org.example.Other", "456"))]);
        var graph = """
            [
              {"id":3,"type":"PipeWire:Interface:Client","info":{"props":{"application.process.id":3,"pipewire.sec.pid":999,"pipewire.access.portal.app_id":"org.example.Player"}}},
              {"id":4,"type":"PipeWire:Interface:Node","info":{"state":"running","props":{"object.serial":104,"client.id":3,"node.name":"radio","media.class":"Stream/Output/Audio"}}},
              {"id":5,"type":"PipeWire:Interface:Client","info":{"props":{"application.process.id":2,"pipewire.sec.pid":999,"pipewire.access.portal.app_id":"org.example.Other"}}},
              {"id":6,"type":"PipeWire:Interface:Node","info":{"state":"running","props":{"object.serial":106,"client.id":5,"node.name":"other","media.class":"Stream/Output/Audio"}}}
            ]
            """;
        Assert.Throws<InvalidOperationException>(() => LinuxPlayerAudio.FindTarget(graph, 900));
        Assert.Equal(new LinuxAudioTarget("104", 501, "radio"), LinuxPlayerAudio.FindTarget(graph, 900, sandbox: selection));
        Assert.Equal(500, selection.Resolve(2, 500, "", ""));
        Assert.Null(selection.Resolve(2, 600, identity.ApplicationId, ""));
        Assert.Null(selection.Resolve(2, 999, "org.example.Other", ""));
        Assert.Null(selection.Resolve(2, 999, identity.ApplicationId, "wrong-instance"));
        Assert.Null(selection.Resolve(999, 999, identity.ApplicationId, ""));
        var ambiguous = new LinuxSandboxSelection(identity,
            [.. selection.Processes, new(700, 2, new(identity.ApplicationId, "789"))]);
        Assert.Throws<InvalidOperationException>(() => ambiguous.Resolve(2, 999, identity.ApplicationId, ""));
        Assert.Equal(500, ambiguous.Resolve(2, 999, identity.ApplicationId, identity.InstanceId));
        Assert.Equal(identity, LinuxSandboxSelection.ParseIdentity("[Application]\nname=org.example.Player\n[Instance]\ninstance-id=123\n"));
        Assert.Null(LinuxSandboxSelection.ParseIdentity("[Application]\nname=org.example.Player\n"));
    }

    [Fact]
    public void NativeClientProcessMetadataIsUsedBeforeThePulseServerProcess()
    {
        var graph = Graph.Replace("\"pipewire.sec.pid\":42", "\"application.process.id\":42,\"pipewire.sec.pid\":999");
        Assert.Equal("104", LinuxPlayerAudio.FindTarget(graph, 42).Serial);
    }

    [Fact]
    public void OnlySelectedPlaybackProcessIsCapturedAndDefaultRoutingIsForbidden()
    {
        Assert.Equal(new LinuxAudioTarget("104", 42, "radio"), LinuxPlayerAudio.FindTarget(Graph, 42));
        Assert.Equal("105", LinuxPlayerAudio.FindTarget(Graph, 99).Serial);
        Assert.Throws<InvalidOperationException>(() => LinuxPlayerAudio.FindTarget(Graph, 123));
        var arguments = LinuxPlayerAudio.CaptureArguments(new("104", 42, "radio"));
        Assert.Contains("--target=104", arguments);
        Assert.Contains(arguments, argument => argument.Contains("node.dont-fallback=true") &&
            argument.Contains("node.dont-reconnect=true") && argument.Contains("node.dont-move=true"));
        Assert.DoesNotContain("--target=auto", arguments);
        Assert.Equal(42, LinuxPlayerAudio.ParseProcessId("method return\n   uint32 42\n"));
        Assert.Throws<InvalidOperationException>(() => LinuxPlayerAudio.ParseProcessId("uint32 0"));
    }

    [Fact]
    public void ProcessChildrenAreMatchedButAmbiguousStreamsAreRejected()
    {
        Assert.Equal("104", LinuxPlayerAudio.FindTarget(Graph, 1, (process, parent) => process == 42 && parent == 1).Serial);
        var duplicate = Graph.Replace("\"application.process.id\":99", "\"application.process.id\":42");
        Assert.Throws<InvalidOperationException>(() => LinuxPlayerAudio.FindTarget(duplicate, 42));
        var idle = duplicate.Replace("\"state\":\"running\",\"props\":{\"object.serial\":105", "\"state\":\"idle\",\"props\":{\"object.serial\":105");
        Assert.Equal("104", LinuxPlayerAudio.FindTarget(idle, 42).Serial);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RawPcmFlagFollowsTheToolCapability(bool explicitRaw)
    {
        var arguments = LinuxPlayerAudio.CaptureArguments(new("104", 42, "radio"), explicitRaw);
        Assert.Equal(explicitRaw, arguments.Contains("--raw"));
        Assert.Contains("--format=s16", arguments);
        Assert.Equal("-", arguments.Last());
    }

    [Fact]
    public async Task AudioValuesFeedVfdAndSwitchingPlayerClearsLevelsImmediately()
    {
        var levels = new AudioLevels(4410, 128, 64, Enumerable.Range(0, 20).Select(index => index * 10).ToArray());
        var audioValues = LinuxPlayerAudio.Map(levels, new("104", 42, "radio"));
        await using var pages = new PageSourceValues();
        pages.UpdateMedia("Media", new Dictionary<string, object?>(audioValues) { ["Connected"] = true, ["Title"] = "Track" });
        pages.SetGlyphNames([], true);
        var snapshot = pages.Snapshot(DateTimeOffset.Now);
        Assert.Equal(20, Assert.IsType<string>(snapshot["Media.SpectrumGlyphs"]).Length);
        Assert.Equal(LIS2.Display.GlyphRendering.Spectrum(levels.Spectrum, 255), snapshot["Media.SpectrumGlyphs"]);
        Assert.Equal(128, snapshot["Media.VuLeft"]);
        Assert.Equal("Track", snapshot["Media.Title"]);
        var quiet = Enumerable.Repeat(8, 20).ToArray();
        pages.UpdateMedia("Media", LinuxPlayerAudio.Map(new AudioLevels(4410, 8, 4, quiet), new("104", 42, "radio")));
        Assert.Equal(LIS2.Display.GlyphRendering.Spectrum(quiet, 255), pages.Snapshot(DateTimeOffset.Now)["Media.SpectrumGlyphs"]);
        await using var audio = new LinuxPlayerAudio();
        audio.SelectPlayer("radio", true);
        Assert.False((bool)audio.Values["AudioConnected"]!);
        audio.SelectPlayer(null, false);
        Assert.Equal("Waiting for playback", audio.Values["AudioStatus"]);
        Assert.Equal("", audio.Values["Vu"]);
    }

    [Theory]
    [InlineData("native")]
    [InlineData("pulse")]
    [InlineData("sandbox")]
    public async Task RealPipeWireCaptureSurvivesStreamReplacementAndPauseWithoutUsingSystemAudio(string mode)
    {
        if (!OperatingSystem.IsLinux()) return;
        var start = new ProcessStartInfo("dbus-run-session")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true
        };
        start.ArgumentList.Add("--");
        start.ArgumentList.Add("/usr/bin/python3");
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "TestData", "linux_audio_service.py"));
        start.ArgumentList.Add(mode);
        using var fixture = Process.Start(start)!;
        var errors = fixture.StandardError.ReadToEndAsync();
        try
        {
            var line = await fixture.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(20));
            Assert.False(string.IsNullOrEmpty(line), "Private audio fixture failed: " + (fixture.HasExited ? await errors : "no startup output"));
            using var ready = JsonDocument.Parse(line!);
            var environment = new Dictionary<string, string>
            {
                ["DBUS_SESSION_BUS_ADDRESS"] = ready.RootElement.GetProperty("address").GetString()!,
                ["XDG_RUNTIME_DIR"] = ready.RootElement.GetProperty("runtime").GetString()!,
                ["PIPEWIRE_RUNTIME_DIR"] = ready.RootElement.GetProperty("runtime").GetString()!
            };
            await using var audio = new LinuxPlayerAudio(environment);
            audio.SelectPlayer("lis2audio", true);
            audio.Start();
            await WaitAudio(audio, values => values.GetValueOrDefault("AudioConnected") is true);
            var first = audio.Values;
            Assert.Contains(mode == "sandbox" ? "; Flatpak: org.example.Lis2Audio" : "; host process", audio.IdentityDetails);
            Assert.True(Directory.Exists("/proc/" + first["AudioProcessId"]));
            Assert.InRange((int)first["VuLeft"]!, 115, 135);
            Assert.InRange((int)first["VuRight"]!, 50, 75);
            var firstSpectrum = Assert.IsType<int[]>(first["SpectrumRaw"]);
            var peak = Array.IndexOf(firstSpectrum, firstSpectrum.Max());
            Assert.InRange(peak, 9, 11);
            var serial = (string)first["AudioNodeSerial"]!;
            await fixture.StandardInput.WriteLineAsync("restart");
            await fixture.StandardInput.FlushAsync();
            await fixture.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5));
            await WaitAudio(audio, values => values.GetValueOrDefault("AudioConnected") is true &&
                !Equals(serial, values.GetValueOrDefault("AudioNodeSerial")));
            var nextSpectrum = Assert.IsType<int[]>(audio.Values["SpectrumRaw"]);
            Assert.InRange(Array.IndexOf(nextSpectrum, nextSpectrum.Max()), 14, 16);
            await fixture.StandardInput.WriteLineAsync("silence");
            await fixture.StandardInput.FlushAsync();
            await fixture.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5));
            await WaitAudio(audio, values => values.GetValueOrDefault("AudioConnected") is false);
            Assert.Equal("", audio.Values["Spectrum"]);
            await fixture.StandardInput.WriteLineAsync("resume");
            await fixture.StandardInput.FlushAsync();
            await fixture.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5));
            await WaitAudio(audio, values => values.GetValueOrDefault("AudioConnected") is true);
            await fixture.StandardInput.WriteLineAsync("pause");
            await fixture.StandardInput.FlushAsync();
            await fixture.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5));
            audio.SelectPlayer("lis2audio", false);
            Assert.False((bool)audio.Values["AudioConnected"]!);
            Assert.Equal("", audio.Values["Spectrum"]);
            await fixture.StandardInput.WriteLineAsync("resume");
            await fixture.StandardInput.FlushAsync();
            await fixture.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5));
            audio.SelectPlayer("lis2audio", true);
            await WaitAudio(audio, values => values.GetValueOrDefault("AudioConnected") is true);
            audio.SelectPlayer("absent", true);
            Assert.False((bool)audio.Values["AudioConnected"]!);
            await Task.Delay(1200);
            Assert.False((bool)audio.Values["AudioConnected"]!);
            Assert.Equal("", audio.Values["Vu"]);
        }
        finally
        {
            if (!fixture.HasExited)
            {
                await fixture.StandardInput.WriteLineAsync("stop");
                await fixture.StandardInput.FlushAsync();
                try { await fixture.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); }
                catch (TimeoutException) { fixture.Kill(entireProcessTree: true); await fixture.WaitForExitAsync(); }
            }
            await errors;
        }
    }

    private static async Task WaitAudio(LinuxPlayerAudio source, Func<IReadOnlyDictionary<string, object?>, bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!condition(source.Values) && watch.Elapsed < TimeSpan.FromSeconds(15)) await Task.Delay(50);
        Assert.True(condition(source.Values), "Audio did not reach expected state: " + source.Values.GetValueOrDefault("AudioStatus"));
    }
}
