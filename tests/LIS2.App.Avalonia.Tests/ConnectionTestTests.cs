using System.Net.Sockets;
using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class ConnectionTestTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SerialChecksNeverOpenOrWriteAndStopAtMissingPrerequisites(bool connected)
    {
        var discoveries = 0;
        IReadOnlyList<string> Discover() { discoveries++; return ["test-port"]; }
        var missing = await Run(ConnectionTestPlans.Serial(null, false, Discover));
        Assert.Equal(ConnectionTestState.Failed, missing[0].State);
        Assert.Equal(ConnectionTestState.Skipped, missing[1].State);
        Assert.Equal(0, discoveries);
        var result = await Run(ConnectionTestPlans.Serial("test-port", connected, Discover));
        Assert.Equal(1, discoveries);
        Assert.Equal(ConnectionTestState.Passed, result[1].State);
        Assert.Equal(connected ? ConnectionTestState.Passed : ConnectionTestState.Failed, result[2].State);
        var absent = await Run(ConnectionTestPlans.Serial("absent-port", true, Discover));
        Assert.Equal(ConnectionTestState.Failed, absent[1].State);
        Assert.Equal(ConnectionTestState.Skipped, absent[2].State);
    }

    [Theory]
    [InlineData("Winamp")]
    [InlineData("WmpLegacy")]
    [InlineData("WindowsMedia")]
    [InlineData("Media")]
    public async Task MetadataAudioAndDependenciesAreDistinguished(string source)
    {
        var values = new Dictionary<string, object?> { ["Connected"] = true, ["State"] = "Playing",
            ["SnapshotAgeSeconds"] = 0.0, ["VuLeft"] = (byte)0, ["VuRight"] = (byte)0, ["VisualizationConnected"] = true };
        var audio = new Dictionary<string, object?> { ["AudioConnected"] = true };
        var valid = await Run(ConnectionTestPlans.Media(source, values, audio, true, _ => true));
        Assert.All(valid, result => Assert.Equal(ConnectionTestState.Passed, result.State));
        values["Connected"] = false;
        var disconnected = await Run(ConnectionTestPlans.Media(source, values, audio, true, _ => true));
        Assert.Contains(disconnected, result => result.State == ConnectionTestState.Failed && result.Detail == "No connection to the selected player");
        values["Connected"] = true;
        if (source != "WindowsMedia")
        {
            values["SnapshotAgeSeconds"] = double.NaN;
            var stale = await Run(ConnectionTestPlans.Media(source, values, audio, true, _ => true));
            Assert.Contains(stale, result => result.Detail == "Player metadata is stale");
            values["SnapshotAgeSeconds"] = 0;
            values["VuLeft"] = null; values["VisualizationConnected"] = false; audio["AudioConnected"] = false;
            var noAudio = await Run(ConnectionTestPlans.Media(source, values, audio, true, _ => true));
            Assert.Equal("No current audio from the selected player", noAudio.Last().Detail);
            values["State"] = "Paused";
            var paused = await Run(ConnectionTestPlans.Media(source, values, audio, true, _ => true));
            Assert.Equal(ConnectionTestState.Skipped, paused.Last().State);
            Assert.DoesNotContain(paused, result => result.State == ConnectionTestState.Failed);
        }
        if (source == "Media")
        {
            var missing = await Run(ConnectionTestPlans.Media(source, values, audio, true, _ => false));
            Assert.Equal("playerctl is missing", missing[0].Detail);
            values["State"] = "Playing";
            var audioTools = await Run(ConnectionTestPlans.Media(source, values, audio, true, name => name == "playerctl"));
            Assert.Contains(audioTools, result => result.Detail == "Required Linux audio tools are missing");
        }
        if (source == "WindowsMedia")
            Assert.Equal("The Windows media provider is unavailable", (await Run(ConnectionTestPlans.Media(source, values, audio, false, _ => true)))[0].Detail);
    }

    [Fact]
    public async Task HardwareRequiresActualFiniteFreshSensorValues()
    {
        Assert.Equal(ConnectionTestState.Failed, (await Run(ConnectionTestPlans.Hardware(new Dictionary<string, object?> { ["Hardware.Cpu.Temperature"] = double.NaN }, true)))[0].State);
        var values = new Dictionary<string, object?> { ["Hardware.Cpu.Temperature"] = 0.0 };
        Assert.All(await Run(ConnectionTestPlans.Hardware(values, true)), result => Assert.Equal(ConnectionTestState.Passed, result.State));
        Assert.Equal("Hardware measurements are stale", (await Run(ConnectionTestPlans.Hardware(values, false)))[1].Detail);
    }

    [Theory]
    [InlineData("pw-dump")]
    [InlineData("pw-record")]
    [InlineData("dbus-send")]
    public async Task LinuxIdentifiesTheMissingExecutableUsedByTheActualAudioProvider(string missing)
    {
        var checkedNames = new HashSet<string>();
        var values = new Dictionary<string, object?> { ["Connected"] = true, ["State"] = "Playing", ["SnapshotAgeSeconds"] = 0 };
        var result = await Run(ConnectionTestPlans.Media("Media", values, new Dictionary<string, object?> { ["AudioConnected"] = true }, true,
            executable => { checkedNames.Add(executable); return executable != missing; }));
        Assert.Equal(missing, Assert.Single(result, step => step.State == ConnectionTestState.Failed).Name);
        Assert.Equal(new[] { "dbus-send", "playerctl", "pw-dump", "pw-record" }, checkedNames.OrderBy(name => name).ToArray());
    }

    [Fact]
    public async Task TimeoutCancellationAndErrorsDoNotExposeCredentialsOrRunLaterSteps()
    {
        const string secret = "fixture-secret-http://example.invalid?token=private";
        var later = false;
        ConnectionTestStep[] steps = [new("first", "remedy", _ => throw new UnauthorizedAccessException(secret)),
            new("second", "", _ => { later = true; return Task.FromResult<string?>(null); })];
        var denied = await Run(steps);
        Assert.Equal("Access denied", denied[0].Detail); Assert.False(later);
        Assert.DoesNotContain(secret, string.Join("|", denied));
        steps[0] = new("first", "remedy", async token => { await Task.Delay(Timeout.InfiniteTimeSpan, token); return null; });
        var timed = await ConnectionTest.RunAsync(steps, _ => { }, CancellationToken.None, TimeSpan.FromMilliseconds(30));
        Assert.Equal("Connection test timed out", timed[0].Detail); Assert.False(later);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        var cancelled = await ConnectionTest.RunAsync(steps, _ => { }, cancel.Token);
        Assert.Equal(ConnectionTestState.Cancelled, cancelled[0].State);
        Assert.Equal(ConnectionTestState.Skipped, cancelled[1].State);
        Assert.DoesNotContain(secret, ConnectionTest.SafeError(new InvalidOperationException(secret)));
        Assert.Contains("HostNotFound", ConnectionTest.SafeError(new SocketException((int)SocketError.HostNotFound)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public async Task RealHomeAssistantHandshakeIdentifiesAuthenticationFailureAndTimeout(bool? authenticate)
    {
        await using var server = new HomeAssistantServer();
        var probe = new HomeAssistantConnectionProbe(server.Url, "fixture-token");
        var run = Run(probe.Steps());
        await server.AcceptAsync(authenticate, "fixture-token https://secret.invalid/?password=hidden");
        var results = await run;
        Assert.All(results.Take(4), result => Assert.Equal(ConnectionTestState.Passed, result.State));
        Assert.Equal(authenticate is true ? ConnectionTestState.Passed : ConnectionTestState.Failed, results[4].State);
        if (authenticate is null) Assert.Equal("Connection test timed out", results[4].Detail);
        Assert.DoesNotContain("fixture-token", string.Join("|", results));
        Assert.DoesNotContain("secret.invalid", string.Join("|", results));
        if (authenticate is true)
        {
            var retry = Run(probe.Steps());
            await server.AcceptAsync(false);
            Assert.Equal(ConnectionTestState.Failed, (await retry)[4].State);
        }
    }

    [Theory]
    [InlineData("not-a-url", "fixture-token", 0)]
    [InlineData("https://example.invalid", "", 1)]
    public async Task InvalidHomeAssistantConfigurationStopsBeforeNetwork(string address, string token, int failed)
    {
        var results = await Run(new HomeAssistantConnectionProbe(address, token).Steps());
        Assert.Equal(ConnectionTestState.Failed, results[failed].State);
        Assert.All(results.Skip(failed + 1), result => Assert.Equal(ConnectionTestState.Skipped, result.State));
    }

    [Fact]
    public async Task RealWebSocketFailureAndWrongGreetingIdentifyDifferentStages()
    {
        using var closedEndpoint = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        closedEndpoint.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
        var address = "http://127.0.0.1:" + ((System.Net.IPEndPoint)closedEndpoint.LocalEndPoint!).Port;
        var refused = await Run(new HomeAssistantConnectionProbe(address, "fixture-token").Steps());
        Assert.Equal(ConnectionTestState.Failed, refused[2].State);
        Assert.Equal(ConnectionTestState.Skipped, refused[3].State);
        await using var server = new HomeAssistantServer();
        var run = Run(new HomeAssistantConnectionProbe(server.Url, "fixture-token").Steps());
        await server.AcceptAsync(greetingType: "unexpected");
        var greeting = await run;
        Assert.Equal(ConnectionTestState.Passed, greeting[2].State);
        Assert.Equal("Unexpected Home Assistant greeting", greeting[3].Detail);
        Assert.Equal(ConnectionTestState.Skipped, greeting[4].State);
    }

    private static Task<IReadOnlyList<ConnectionTestResult>> Run(IReadOnlyList<ConnectionTestStep> steps) =>
        ConnectionTest.RunAsync(steps, _ => { }, CancellationToken.None);
}
