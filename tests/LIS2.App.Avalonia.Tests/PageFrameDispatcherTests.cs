using LIS2.App.Avalonia;
using LIS2.Display;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageFrameDispatcherTests
{
    [Fact]
    public async Task IdenticalFramesAreWrittenOnlyOnce()
    {
        var writes = new List<DisplayFrame>();
        var clears = 0;
        var dispatcher = new PageFrameDispatcher(
            (frame, _) => { writes.Add(frame); return Task.CompletedTask; },
            _ => { clears++; return Task.CompletedTask; });
        var frame = DisplayFrame.Create("One", "Two");
        Assert.True(await dispatcher.DispatchAsync(frame));
        Assert.False(await dispatcher.DispatchAsync(DisplayFrame.Create("One", "Two")));
        Assert.Single(writes);
        Assert.Equal(0, clears);
    }

    [Fact]
    public async Task MissingPageClearsOnlyOnceAndNewFrameRestarts()
    {
        var writes = 0;
        var clears = 0;
        var dispatcher = new PageFrameDispatcher(
            (_, _) => { writes++; return Task.CompletedTask; },
            _ => { clears++; return Task.CompletedTask; });
        Assert.False(await dispatcher.DispatchAsync(null));
        Assert.True(await dispatcher.DispatchAsync(DisplayFrame.Create("A", "B")));
        Assert.True(await dispatcher.DispatchAsync(null));
        Assert.False(await dispatcher.DispatchAsync(null));
        Assert.True(await dispatcher.DispatchAsync(DisplayFrame.Create("A", "B")));
        Assert.Equal(2, writes);
        Assert.Equal(1, clears);
    }

    [Fact]
    public async Task FailedWriteDoesNotCacheFrame()
    {
        var attempts = 0;
        var dispatcher = new PageFrameDispatcher(
            (_, _) =>
            {
                if (++attempts == 1) throw new IOException("Device unplugged");
                return Task.CompletedTask;
            },
            _ => Task.CompletedTask);
        var frame = DisplayFrame.Create("A", "B");
        await Assert.ThrowsAsync<IOException>(() => dispatcher.DispatchAsync(frame));
        Assert.True(await dispatcher.DispatchAsync(frame));
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ConcurrentDispatchesNeverOverlap()
    {
        var inFlight = 0;
        var maximum = 0;
        var dispatcher = new PageFrameDispatcher(
            async (_, _) =>
            {
                var count = Interlocked.Increment(ref inFlight);
                maximum = Math.Max(maximum, count);
                await Task.Delay(10);
                Interlocked.Decrement(ref inFlight);
            },
            _ => Task.CompletedTask);
        await Task.WhenAll(Enumerable.Range(0, 6).Select(i =>
            dispatcher.DispatchAsync(DisplayFrame.Create(i.ToString(), ""))));
        Assert.Equal(1, maximum);
    }
}
