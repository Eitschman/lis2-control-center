using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class AdaptiveRuntimeLoopTests
{
    [Fact]
    public async Task StopsAndDrainsInFlightTick()
    {
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var loop = new AdaptiveRuntimeLoop(async _ =>
        {
            entered.TrySetResult(true);
            await release.Task;
            return TimeSpan.FromMilliseconds(5);
        });
        Assert.True(loop.Start());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var drain = loop.StopAndDrainAsync();
        Assert.False(drain.IsCompleted);
        Assert.False(loop.Start());
        release.SetResult(true);
        await drain;
        Assert.False(loop.IsRunning);
        Assert.True(loop.Start());
        await loop.StopAndDrainAsync();
    }

    [Fact]
    public async Task FailureCanBeObservedAndRestarted()
    {
        var attempts = 0;
        var firstAttempt = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var restartedAttempt = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var loop = new AdaptiveRuntimeLoop(_ =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
            {
                firstAttempt.TrySetResult(true);
                throw new IOException("failure");
            }
            restartedAttempt.TrySetResult(true);
            return Task.FromResult(TimeSpan.FromMilliseconds(10));
        });
        Assert.True(loop.Start());
        await firstAttempt.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAsync<IOException>(() => loop.StopAndDrainAsync());
        Assert.True(loop.Start());
        await restartedAttempt.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await loop.StopAndDrainAsync();
        Assert.True(attempts > 1);
    }
}
