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
    [Fact]
    public async Task StopAndDrainWaitsForPendingWrite()
    {
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var runtime = new PageSequenceController(
            async (_, _) => { entered.SetResult(true); await release.Task; },
            _ => Task.CompletedTask);
        runtime.Start(new[] { new PageDefinition { Line1Template = "HELLO" } });
        var pending = runtime.TickAsync(DateTimeOffset.UnixEpoch, new Dictionary<string, object?>());
        await entered.Task;
        var drain = runtime.StopAndDrainAsync();
        Assert.False(drain.IsCompleted);
        Assert.False(runtime.IsRunning);
        release.SetResult(true);
        await pending;
        await drain;
        Assert.False(runtime.IsBusy);
        Assert.True(runtime.Start(new[] { new PageDefinition { Line1Template = "NEXT" } }));
    }

    [Fact]
    public async Task StopAndDrainIsSafeWithoutActiveSequence()
    {
        var runtime = new PageSequenceController((_, _) => Task.CompletedTask, _ => Task.CompletedTask);
        await runtime.StopAndDrainAsync();
        Assert.False(runtime.IsRunning);
    }

    [Fact]
    public async Task PeriodicRuntimeLoopStopsWithoutFurtherCallbacks()
    {
        var count = 0;
        await using var loop = new PeriodicRuntimeLoop(TimeSpan.FromMilliseconds(10),
            _ => { Interlocked.Increment(ref count); return Task.CompletedTask; });
        Assert.True(loop.Start());
        Assert.False(loop.Start());
        await Task.Delay(75);
        await loop.StopAndDrainAsync();
        var stoppedAt = Volatile.Read(ref count);
        Assert.True(stoppedAt > 0);
        await Task.Delay(40);
        Assert.Equal(stoppedAt, Volatile.Read(ref count));
        Assert.True(loop.Start());
        await Task.Delay(30);
        await loop.StopAndDrainAsync();
        Assert.True(Volatile.Read(ref count) > stoppedAt);
    }

    [Fact]
    public async Task PeriodicRuntimeLoopDrainsInFlightCallback()
    {
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var loop = new PeriodicRuntimeLoop(TimeSpan.FromMilliseconds(10),
            async _ => { entered.TrySetResult(true); await release.Task; });
        Assert.True(loop.Start());
        await entered.Task;
        var drain = loop.StopAndDrainAsync();
        Assert.False(drain.IsCompleted);
        Assert.False(loop.Start());
        release.SetResult(true);
        await drain;
        Assert.False(loop.IsRunning);
    }
    [Fact]
    public async Task PeriodicRuntimeLoopCanRestartAfterCallbackFailureWasObserved()
    {
        var attempts = 0;
        await using var loop = new PeriodicRuntimeLoop(TimeSpan.FromMilliseconds(10),
            _ =>
            {
                if (Interlocked.Increment(ref attempts) == 1)
                    throw new IOException("Simulated source failure");
                return Task.CompletedTask;
            });
        Assert.True(loop.Start());
        await Task.Delay(70);
        await Assert.ThrowsAsync<IOException>(() => loop.StopAndDrainAsync());
        Assert.False(loop.IsRunning);
        Assert.True(loop.Start());
        await Task.Delay(50);
        await loop.StopAndDrainAsync();
        Assert.True(Volatile.Read(ref attempts) > 1);
    }
}
