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
    public async Task SharedPriorityEventsOverridePageAndExpireWithoutResettingPage()
    {
        var now = DateTimeOffset.UnixEpoch;
        var frames = new List<DisplayFrame>();
        await using var runtime = new DisplayRuntimeHost(clock: () => now);
        var sequence = runtime.CreateSequence((frame, _) => { frames.Add(frame); return Task.CompletedTask; }, _ => Task.CompletedTask);
        sequence.Start(new[] { new PageDefinition { Id = "page", Line1Template = "NORMAL", DurationSeconds = 10 } });
        Assert.True(await runtime.TickAsync(sequence));
        runtime.EnqueueEvent(new DisplayEvent("low", new DisplayFrame("LOW", ""), 1, now.AddSeconds(3)));
        runtime.EnqueueEvent(new DisplayEvent("high", new DisplayFrame("HIGH", ""), 10, now.AddSeconds(2)));
        Assert.True(await runtime.TickAsync(sequence));
        Assert.StartsWith("HIGH", frames[^1].Line1);
        now = now.AddSeconds(2);
        Assert.True(await runtime.TickAsync(sequence));
        Assert.StartsWith("LOW", frames[^1].Line1);
        now = now.AddSeconds(2);
        Assert.True(await runtime.TickAsync(sequence));
        Assert.StartsWith("NORMAL", frames[^1].Line1);
        Assert.Equal("page", sequence.CurrentPageId);
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

    [Fact]
    public async Task SharedEventsReachBothTransportsAndNormalPagesResume()
    {
        var now = DateTimeOffset.UnixEpoch;
        var virtualFrames = new List<string>();
        var serialFrames = new List<string>();
        await using var runtime = new DisplayRuntimeHost(clock: () => now);
        var virtualSequence = runtime.CreateSequence(
            (frame, _) => { virtualFrames.Add(frame.Line1.Trim()); return Task.CompletedTask; },
            _ => Task.CompletedTask);
        var serialSequence = runtime.CreateSequence(
            (frame, _) => { serialFrames.Add(frame.Line1.Trim()); return Task.CompletedTask; },
            _ => Task.CompletedTask);
        var pages = new[] { new PageDefinition { Line1Template = "NORMAL", DurationSeconds = 10 } };
        Assert.True(virtualSequence.Start(pages));
        Assert.True(serialSequence.Start(pages));
        await runtime.TickAsync(virtualSequence);
        await runtime.TickAsync(serialSequence);
        runtime.EnqueueEvent(new DisplayEvent("alert", DisplayFrame.Create("ALERT", ""), 100, now.AddSeconds(3)));
        await runtime.TickAsync(virtualSequence);
        await runtime.TickAsync(serialSequence);
        Assert.Equal("ALERT", virtualFrames[^1]);
        Assert.Equal("ALERT", serialFrames[^1]);
        now = now.AddSeconds(4);
        await runtime.TickAsync(virtualSequence);
        await runtime.TickAsync(serialSequence);
        Assert.Equal(new[] { "NORMAL", "ALERT", "NORMAL" }, virtualFrames);
        Assert.Equal(new[] { "NORMAL", "ALERT", "NORMAL" }, serialFrames);
        Assert.True(virtualSequence.IsRunning);
        Assert.True(serialSequence.IsRunning);
    }

    [Fact]
    public async Task EventOnlySequenceClearsAfterExpiryWithoutShowingRegularPages()
    {
        var now = DateTimeOffset.UnixEpoch;
        var written = new List<string>();
        var clears = 0;
        await using var runtime = new DisplayRuntimeHost(clock: () => now);
        var sequence = runtime.CreateSequence(
            (frame, _) => { written.Add(frame.Line1.Trim()); return Task.CompletedTask; },
            _ => { clears++; return Task.CompletedTask; });
        runtime.EnqueueEvent(new DisplayEvent("one", DisplayFrame.Create("ALERT", ""), 100, now.AddSeconds(2)));
        Assert.True(sequence.Start(Array.Empty<PageDefinition>()));
        Assert.True(await runtime.TickAsync(sequence));
        Assert.Equal("ALERT", written.Single());
        now = now.AddSeconds(3);
        Assert.True(await runtime.TickAsync(sequence));
        Assert.Equal(1, clears);
        await sequence.StopAndDrainAsync();
        Assert.False(sequence.IsRunning);
    }

    [Fact]
    public async Task HigherPriorityEventPreemptsOnBothSequencesAndCancelRestoresLowerPriority()
    {
        var now = DateTimeOffset.UnixEpoch;
        var virtualFrames = new List<string>();
        var serialFrames = new List<string>();
        await using var runtime = new DisplayRuntimeHost(clock: () => now);
        var virtualSequence = runtime.CreateSequence(
            (frame, _) => { virtualFrames.Add(frame.Line1.Trim()); return Task.CompletedTask; },
            _ => Task.CompletedTask);
        var serialSequence = runtime.CreateSequence(
            (frame, _) => { serialFrames.Add(frame.Line1.Trim()); return Task.CompletedTask; },
            _ => Task.CompletedTask);
        Assert.True(virtualSequence.Start(Array.Empty<PageDefinition>()));
        Assert.True(serialSequence.Start(Array.Empty<PageDefinition>()));
        runtime.EnqueueEvent(new DisplayEvent("low", DisplayFrame.Create("LOW", ""), 10, now.AddSeconds(20)));
        runtime.EnqueueEvent(new DisplayEvent("high", DisplayFrame.Create("HIGH", ""), 100, now.AddSeconds(20)));
        await runtime.TickAsync(virtualSequence);
        await runtime.TickAsync(serialSequence);
        Assert.Equal("HIGH", virtualFrames[^1]);
        Assert.Equal("HIGH", serialFrames[^1]);
        Assert.True(runtime.Events.Remove("high"));
        await runtime.TickAsync(virtualSequence);
        await runtime.TickAsync(serialSequence);
        Assert.Equal("LOW", virtualFrames[^1]);
        Assert.Equal("LOW", serialFrames[^1]);
    }

    [Fact]
    public async Task EventOnlySequenceCanBePromotedToPagePlaybackWithoutLosingOverlay()
    {
        var now = DateTimeOffset.UnixEpoch;
        var writes = new List<string>();
        await using var runtime = new DisplayRuntimeHost(clock: () => now);
        var sequence = runtime.CreateSequence(
            (frame, _) => { writes.Add(frame.Line1.Trim()); return Task.CompletedTask; },
            _ => Task.CompletedTask);
        runtime.EnqueueEvent(new DisplayEvent("alert", DisplayFrame.Create("ALERT", ""), 100, now.AddSeconds(5)));
        Assert.True(sequence.Start(Array.Empty<PageDefinition>()));
        Assert.True(await runtime.TickAsync(sequence));
        Assert.True(await sequence.ReloadAsync(new[]
        {
            new PageDefinition { Id = "normal", Line1Template = "NORMAL", DurationSeconds = 20 }
        }));
        Assert.False(await runtime.TickAsync(sequence));
        Assert.Equal(new[] { "ALERT" }, writes);
        now = now.AddSeconds(6);
        Assert.True(await runtime.TickAsync(sequence));
        Assert.Equal(new[] { "ALERT", "NORMAL" }, writes);
        Assert.Equal("normal", sequence.CurrentPageId);
        Assert.True(sequence.IsRunning);
    }

    [Fact]
    public async Task BothEventOnlyTransportsCanPromoteIndependently()
    {
        var now = DateTimeOffset.UnixEpoch;
        await using var runtime = new DisplayRuntimeHost(clock: () => now);
        var virtualFrames = new List<string>();
        var serialFrames = new List<string>();
        var virtualSequence = runtime.CreateSequence(
            (frame, _) => { virtualFrames.Add(frame.Line1.Trim()); return Task.CompletedTask; },
            _ => Task.CompletedTask);
        var serialSequence = runtime.CreateSequence(
            (frame, _) => { serialFrames.Add(frame.Line1.Trim()); return Task.CompletedTask; },
            _ => Task.CompletedTask);
        runtime.EnqueueEvent(new DisplayEvent("alert", DisplayFrame.Create("ALERT", ""), 50, now.AddSeconds(2)));
        Assert.True(virtualSequence.Start(Array.Empty<PageDefinition>()));
        Assert.True(serialSequence.Start(Array.Empty<PageDefinition>()));
        await runtime.TickAsync(virtualSequence);
        await runtime.TickAsync(serialSequence);
        Assert.True(await virtualSequence.ReloadAsync(new[] { new PageDefinition { Line1Template = "VIRTUAL" } }));
        Assert.True(await serialSequence.ReloadAsync(new[] { new PageDefinition { Line1Template = "SERIAL" } }));
        now = now.AddSeconds(3);
        await runtime.TickAsync(virtualSequence);
        await runtime.TickAsync(serialSequence);
        Assert.Equal(new[] { "ALERT", "VIRTUAL" }, virtualFrames);
        Assert.Equal(new[] { "ALERT", "SERIAL" }, serialFrames);
    }
}
