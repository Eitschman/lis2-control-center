using LIS2.App.Avalonia;
using LIS2.Display;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageSequenceControllerTests
{
    [Fact]
    public async Task StartTickDeduplicateAndStop()
    {
        var writes = new List<DisplayFrame>();
        var runtime = new PageSequenceController(
            (frame, _) => { writes.Add(frame); return Task.CompletedTask; },
            _ => Task.CompletedTask);
        var pages = new[] { new PageDefinition { Id = "one", Line1Template = "HELLO" } };
        var values = new Dictionary<string, object?>();
        Assert.True(runtime.Start(pages));
        Assert.False(runtime.Start(pages));
        Assert.True(await runtime.TickAsync(DateTimeOffset.UnixEpoch, values));
        Assert.False(await runtime.TickAsync(DateTimeOffset.UnixEpoch.AddMilliseconds(100), values));
        Assert.Single(writes);
        Assert.Equal("one", runtime.CurrentPageId);
        runtime.Stop();
        Assert.False(await runtime.TickAsync(DateTimeOffset.UnixEpoch.AddSeconds(1), values));
        Assert.True(runtime.Start(pages));
        Assert.True(await runtime.TickAsync(DateTimeOffset.UnixEpoch, values));
        Assert.Equal(2, writes.Count);
    }

    [Fact]
    public async Task MissingVisiblePageClearsOnce()
    {
        var clears = 0;
        var runtime = new PageSequenceController(
            (_, _) => Task.CompletedTask,
            _ => { clears++; return Task.CompletedTask; });
        runtime.Start(new[] { new PageDefinition { Id = "a", VisibilityExpression = "Media.State=Playing" } });
        var values = new Dictionary<string, object?> { ["Media.State"] = "Playing" };
        Assert.True(await runtime.TickAsync(DateTimeOffset.UnixEpoch, values));
        values["Media.State"] = "Stopped";
        Assert.True(await runtime.TickAsync(DateTimeOffset.UnixEpoch.AddSeconds(1), values));
        Assert.False(await runtime.TickAsync(DateTimeOffset.UnixEpoch.AddSeconds(2), values));
        Assert.Equal(1, clears);
    }

    [Fact]
    public async Task FailedWriteStopsSequence()
    {
        var runtime = new PageSequenceController(
            (_, _) => throw new IOException("Device disconnected"),
            _ => Task.CompletedTask);
        runtime.Start(new[] { new PageDefinition { Line1Template = "A" } });
        await Assert.ThrowsAsync<IOException>(() =>
            runtime.TickAsync(DateTimeOffset.UnixEpoch, new Dictionary<string, object?>()));
        Assert.False(runtime.IsRunning);
        Assert.False(runtime.IsBusy);
    }

    [Fact]
    public async Task StopDuringWritePreventsRestartUntilWriteFinishes()
    {
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var runtime = new PageSequenceController(
            async (_, _) => { entered.SetResult(true); await release.Task; },
            _ => Task.CompletedTask);
        var pages = new[] { new PageDefinition { Line1Template = "A" } };
        Assert.True(runtime.Start(pages));
        var pending = runtime.TickAsync(DateTimeOffset.UnixEpoch, new Dictionary<string, object?>());
        await entered.Task;
        runtime.Stop();
        Assert.False(runtime.Start(pages));
        Assert.False(await runtime.TickAsync(DateTimeOffset.UnixEpoch, new Dictionary<string, object?>()));
        release.SetResult(true);
        await pending;
        Assert.True(runtime.Start(pages));
    }
}
