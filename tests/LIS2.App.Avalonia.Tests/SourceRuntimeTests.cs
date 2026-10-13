using LIS2.Runtime;

namespace LIS2.App.Avalonia.Tests;

public sealed class SourceRuntimeTests
{
    [Fact]
    public async Task StartTwiceKeepsOriginalClockVariablesAndSnapshot()
    {
        await using var runtime = new SourceRuntime();
        await runtime.StartAsync();
        await runtime.StartAsync();
        Assert.Contains("Clock.Time", runtime.Snapshot().Keys);
        Assert.Contains("Clock.Date", runtime.Snapshot().Keys);
        Assert.Contains("Clock.Day", runtime.Snapshot().Keys);
        Assert.Contains("HA", runtime.Errors.Keys);
    }

    [Fact]
    public async Task ConfigureDisconnectedHomeAssistantPreservesClock()
    {
        await using var runtime = new SourceRuntime();
        await runtime.StartAsync();
        await runtime.ConfigureHomeAssistantAsync(null, null);
        Assert.Contains("Clock.Time", runtime.Snapshot().Keys);
        Assert.False(runtime.HomeAssistant.IsConfigured);
    }

    [Fact]
    public async Task ShutdownRejectsRestartAndSnapshot()
    {
        var runtime = new SourceRuntime();
        await runtime.StartAsync();
        await runtime.DisposeAsync();
        await runtime.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => runtime.StartAsync());
        Assert.Throws<ObjectDisposedException>(() => runtime.Snapshot());
    }
}
