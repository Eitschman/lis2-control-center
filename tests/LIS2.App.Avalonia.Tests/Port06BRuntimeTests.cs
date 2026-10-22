using LIS2.App.Avalonia;
using LIS2.Core;
using LIS2.Runtime;

namespace LIS2.App.Avalonia.Tests;

public sealed class Port06BRuntimeTests
{
    [Fact]
    public async Task ConfigurationWaitsForFrameAndReleasesGateAfterFailure()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sequence = new PageSequenceController(async (_, _) =>
        {
            entered.TrySetResult();
            await release.Task;
        }, _ => Task.CompletedTask);
        sequence.Start([new PageDefinition { Id = "page", Line1Template = "{Value}" }]);
        var tick = sequence.TickAsync(DateTimeOffset.UtcNow, new Dictionary<string, object?>());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var applied = false;
        var configuration = sequence.RunExclusiveAsync(() => { applied = true; return Task.CompletedTask; });
        Assert.False(applied);
        release.SetResult();
        await Task.WhenAll(tick, configuration).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(applied);
        Assert.True(sequence.IsRunning);
        Assert.Equal("page", sequence.CurrentPageId);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sequence.RunExclusiveAsync(() =>
            throw new InvalidOperationException("Isolierter Fehler")));
        Assert.True(await sequence.TickAsync(DateTimeOffset.UtcNow.AddSeconds(1), new Dictionary<string, object?> { ["Value"] = "NEXT" })
            .WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task ConfigurationNeverStartsInactiveFanSessions()
    {
        var transport = new VirtualLis2Transport();
        var channels = new PortableFanSettings().ToRuntimeChannels();
        await using var serial = new SerialDisplaySession(transport);
        await serial.ConnectAsync();
        Assert.False(await serial.ConfigureFansAsync(channels));
        Assert.False(serial.FansRunning);
        Assert.Empty(transport.Writes);
        var virtualTransport = new VirtualLis2Transport();
        await using var virtualSession = new AuthorizedFanRuntimeSession(virtualTransport, new SimulatedFanSensors(), channels);
        Assert.False(await virtualSession.ConfigureAsync(channels));
        Assert.False(virtualSession.FansAuthorized);
        Assert.Empty(virtualTransport.Writes);
    }
}
