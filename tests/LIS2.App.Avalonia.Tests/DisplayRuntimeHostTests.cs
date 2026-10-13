using LIS2.App.Avalonia;
using LIS2.Display;

namespace LIS2.App.Avalonia.Tests;

public sealed class DisplayRuntimeHostTests
{
    [Fact]
    public async Task OwnsSourcesAndSequencesWithDeterministicClock()
    {
        var now = DateTimeOffset.UnixEpoch;
        var frames = new List<DisplayFrame>();
        await using var runtime = new DisplayRuntimeHost(clock: () => now);
        await runtime.StartSourcesAsync();
        await runtime.StartSourcesAsync();
        Assert.True(runtime.Snapshot().ContainsKey("Clock.Time"));
        var sequence = runtime.CreateSequence(
            (frame, _) => { frames.Add(frame); return Task.CompletedTask; },
            _ => Task.CompletedTask);
        Assert.True(sequence.Start(new[] { new PageDefinition { Id = "test", Line1Template = "HELLO" } }));
        Assert.True(await runtime.TickAsync(sequence));
        Assert.False(await runtime.TickAsync(sequence));
        Assert.Single(frames);
        Assert.Equal("test", sequence.CurrentPageId);
    }

    [Fact]
    public async Task RejectsForeignSequences()
    {
        await using var runtime = new DisplayRuntimeHost();
        var foreign = new PageSequenceController((_, _) => Task.CompletedTask, _ => Task.CompletedTask);
        await Assert.ThrowsAsync<ArgumentException>(() => runtime.TickAsync(foreign));
    }

    [Fact]
    public async Task DisposingRuntimeStopsAndDrainsOutstandingWrites()
    {
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var runtime = new DisplayRuntimeHost(clock: () => DateTimeOffset.UnixEpoch);
        var sequence = runtime.CreateSequence(
            async (_, _) => { entered.TrySetResult(true); await release.Task; },
            _ => Task.CompletedTask);
        sequence.Start(new[] { new PageDefinition { Line1Template = "HELLO" } });
        var tick = runtime.TickAsync(sequence);
        await entered.Task;
        var dispose = runtime.DisposeAsync().AsTask();
        Assert.False(dispose.IsCompleted);
        Assert.False(sequence.IsRunning);
        release.SetResult(true);
        await tick;
        await dispose;
        Assert.False(sequence.IsBusy);
        Assert.Throws<ObjectDisposedException>(() => runtime.Snapshot());
    }
}
