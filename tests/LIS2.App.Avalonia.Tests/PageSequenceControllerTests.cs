using LIS2.App.Avalonia;
using LIS2.Display;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageSequenceControllerTests
{
    [Fact]
    public async Task ImportedScheduleReloadDropsRemovedPagesAndRespectsDisabledPages()
    {
        var writes = new List<DisplayFrame>();
        var runtime = new PageSequenceController(
            (frame, _) => { writes.Add(frame); return Task.CompletedTask; },
            _ => Task.CompletedTask);
        var workspace = new PageWorkspace();
        workspace.Add().Line1Template = "ORIGINAL";
        Assert.True(runtime.Start(workspace.Pages));
        var now = DateTimeOffset.UnixEpoch;
        var values = new Dictionary<string, object?>();
        Assert.True(await runtime.TickAsync(now, values));

        var imported = new PageWorkspace();
        imported.Add().Line1Template = "IMPORTED";
        var disabled = imported.Add();
        disabled.Line1Template = "DISABLED";
        disabled.Enabled = false;
        workspace.ImportJson(imported.ExportJson());
        Assert.True(await runtime.ReloadAsync(workspace.Pages));
        Assert.True(await runtime.TickAsync(now.AddMilliseconds(1), values));
        Assert.Equal("IMPORTED".PadRight(20), writes[^1].Line1);
        Assert.Equal(workspace.Get(0).Id, runtime.CurrentPageId);
        Assert.True(runtime.IsRunning);
    }


    [Fact]
    public async Task ReloadChangesRunningPageWithoutReconnecting()
    {
        var written = new List<DisplayFrame>();
        var runtime = new PageSequenceController(
            (frame, _) => { written.Add(frame); return Task.CompletedTask; },
            _ => Task.CompletedTask);
        var values = new Dictionary<string, object?>();
        Assert.False(await runtime.ReloadAsync([new PageDefinition { Line1Template = "NO" }]));
        Assert.True(runtime.Start([new PageDefinition { Id = "first", Line1Template = "FIRST" }]));
        Assert.True(await runtime.TickAsync(DateTimeOffset.UnixEpoch, values));
        Assert.True(await runtime.ReloadAsync([new PageDefinition { Id = "second", Line1Template = "SECOND" }]));
        Assert.True(runtime.IsRunning);
        Assert.True(await runtime.TickAsync(DateTimeOffset.UnixEpoch.AddMilliseconds(1), values));
        Assert.Equal("second", runtime.CurrentPageId);
        Assert.Equal("SECOND".PadRight(20), written[^1].Line1);
    }

    [Fact]
    public async Task ReloadWaitsForInflightWrite()
    {
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var runtime = new PageSequenceController(
            async (_, _) => { entered.TrySetResult(true); await release.Task; },
            _ => Task.CompletedTask);
        runtime.Start([new PageDefinition { Line1Template = "OLD" }]);
        var tick = runtime.TickAsync(DateTimeOffset.UnixEpoch, new Dictionary<string, object?>());
        await entered.Task;
        var reload = runtime.ReloadAsync([new PageDefinition { Line1Template = "NEW" }]);
        Assert.False(reload.IsCompleted);
        release.SetResult(true);
        await tick;
        Assert.True(await reload);
        Assert.True(runtime.IsRunning);
    }


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

    [Fact]
    public async Task ReloadWithUnchangedFrameDoesNotRepeatDeviceWrite()
    {
        var writes = 0;
        var runtime = new PageSequenceController(
            (_, _) => { writes++; return Task.CompletedTask; },
            _ => Task.CompletedTask);
        var pages = new[] { new PageDefinition { Id = "same", Line1Template = "SAME" } };
        Assert.True(runtime.Start(pages));
        var values = new Dictionary<string, object?>();
        Assert.True(await runtime.TickAsync(DateTimeOffset.UnixEpoch, values));
        Assert.True(await runtime.ReloadAsync(pages));
        Assert.False(await runtime.TickAsync(DateTimeOffset.UnixEpoch.AddMilliseconds(1), values));
        Assert.Equal(1, writes);
    }

    [Fact]
    public async Task ReloadToEmptyScheduleClearsPreviouslyDisplayedFrameExactlyOnce()
    {
        var writes = 0;
        var clears = 0;
        var runtime = new PageSequenceController(
            (_, _) => { writes++; return Task.CompletedTask; },
            _ => { clears++; return Task.CompletedTask; });
        Assert.True(runtime.Start([new PageDefinition { Line1Template = "OLD" }]));
        var values = new Dictionary<string, object?>();
        Assert.True(await runtime.TickAsync(DateTimeOffset.UnixEpoch, values));
        Assert.True(await runtime.ReloadAsync(Array.Empty<PageDefinition>()));
        Assert.True(await runtime.TickAsync(DateTimeOffset.UnixEpoch.AddMilliseconds(1), values));
        Assert.False(await runtime.TickAsync(DateTimeOffset.UnixEpoch.AddMilliseconds(2), values));
        Assert.Equal(1, writes);
        Assert.Equal(1, clears);
    }

    [Fact]
    public async Task ReloadToDisabledPagesClearsOldFrame()
    {
        var clears = 0;
        var runtime = new PageSequenceController(
            (_, _) => Task.CompletedTask,
            _ => { clears++; return Task.CompletedTask; });
        Assert.True(runtime.Start([new PageDefinition { Line1Template = "VISIBLE" }]));
        var values = new Dictionary<string, object?>();
        await runtime.TickAsync(DateTimeOffset.UnixEpoch, values);
        Assert.True(await runtime.ReloadAsync([
            new PageDefinition { Line1Template = "HIDDEN", Enabled = false }
        ]));
        Assert.True(await runtime.TickAsync(DateTimeOffset.UnixEpoch.AddMilliseconds(1), values));
        Assert.Equal(1, clears);
    }

    [Fact]
    public async Task ReloadToDifferentFrameWritesNewFrameWithoutClearingFirst()
    {
        var writes = new List<string>();
        var clears = 0;
        var runtime = new PageSequenceController(
            (frame, _) => { writes.Add(frame.Line1.TrimEnd()); return Task.CompletedTask; },
            _ => { clears++; return Task.CompletedTask; });
        Assert.True(runtime.Start([new PageDefinition { Line1Template = "OLD" }]));
        var values = new Dictionary<string, object?>();
        await runtime.TickAsync(DateTimeOffset.UnixEpoch, values);
        Assert.True(await runtime.ReloadAsync([new PageDefinition { Line1Template = "NEW" }]));
        Assert.True(await runtime.TickAsync(DateTimeOffset.UnixEpoch.AddMilliseconds(1), values));
        Assert.Equal(new[] { "OLD", "NEW" }, writes);
        Assert.Equal(0, clears);
    }

    [Fact]
    public async Task IndependentVirtualAndSerialSequencesBothAcceptEditedWorkspace()
    {
        var virtualWrites = new List<string>();
        var serialWrites = new List<string>();
        var virtualSequence = new PageSequenceController(
            (frame, _) => { virtualWrites.Add(frame.Line1.TrimEnd()); return Task.CompletedTask; },
            _ => Task.CompletedTask);
        var serialSequence = new PageSequenceController(
            (frame, _) => { serialWrites.Add(frame.Line1.TrimEnd()); return Task.CompletedTask; },
            _ => Task.CompletedTask);
        var workspace = new PageWorkspace();
        workspace.Add().Line1Template = "BEFORE";
        var values = new Dictionary<string, object?>();
        var now = DateTimeOffset.UnixEpoch;
        Assert.True(virtualSequence.Start(workspace.Pages));
        Assert.True(serialSequence.Start(workspace.Pages));
        await virtualSequence.TickAsync(now, values);
        await serialSequence.TickAsync(now, values);
        workspace.Get(0).Line1Template = "AFTER";
        Assert.True(await virtualSequence.ReloadAsync(workspace.Pages));
        Assert.True(await serialSequence.ReloadAsync(workspace.Pages));
        await virtualSequence.TickAsync(now.AddMilliseconds(1), values);
        await serialSequence.TickAsync(now.AddMilliseconds(1), values);
        Assert.Equal(new[] { "BEFORE", "AFTER" }, virtualWrites);
        Assert.Equal(new[] { "BEFORE", "AFTER" }, serialWrites);
        Assert.True(virtualSequence.IsRunning);
        Assert.True(serialSequence.IsRunning);
    }

    [Fact]
    public async Task ReloadingOneTransportDoesNotRestartOrAlterOtherTransport()
    {
        var serialWrites = 0;
        var virtualSequence = new PageSequenceController(
            (_, _) => Task.CompletedTask, _ => Task.CompletedTask);
        var serialSequence = new PageSequenceController(
            (_, _) => { serialWrites++; return Task.CompletedTask; }, _ => Task.CompletedTask);
        var original = new[] { new PageDefinition { Id = "old", Line1Template = "OLD" } };
        var replacement = new[] { new PageDefinition { Id = "new", Line1Template = "NEW" } };
        Assert.True(virtualSequence.Start(original));
        Assert.True(serialSequence.Start(original));
        var now = DateTimeOffset.UnixEpoch;
        var values = new Dictionary<string, object?>();
        await serialSequence.TickAsync(now, values);
        Assert.True(await virtualSequence.ReloadAsync(replacement));
        Assert.Equal("old", serialSequence.CurrentPageId);
        Assert.False(await serialSequence.TickAsync(now.AddMilliseconds(1), values));
        Assert.Equal(1, serialWrites);
        Assert.True(serialSequence.IsRunning);
    }

    [Fact]
    public async Task ManualCommandRunsOnlyAfterScheduledWriteHasDrained()
    {
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operations = new List<string>();
        var runtime = new PageSequenceController(
            async (_, _) =>
            {
                operations.Add("playback-start");
                entered.TrySetResult(true);
                await release.Task;
                operations.Add("playback-end");
            },
            _ => Task.CompletedTask);
        Assert.True(runtime.Start([new PageDefinition { Line1Template = "PAGE" }]));
        var pending = runtime.TickAsync(DateTimeOffset.UnixEpoch, new Dictionary<string, object?>());
        await entered.Task;
        runtime.Stop();
        var manual = Task.Run(async () =>
        {
            await runtime.StopAndDrainAsync();
            operations.Add("manual");
        });
        Assert.False(manual.IsCompleted);
        release.TrySetResult(true);
        await Task.WhenAll(pending, manual);
        Assert.Equal(new[] { "playback-start", "playback-end", "manual" }, operations);
        Assert.False(runtime.IsRunning);
    }

    [Fact]
    public async Task FailedScheduledWriteCanBeDrainedBeforeManualRecovery()
    {
        var runtime = new PageSequenceController(
            (_, _) => throw new IOException("Simulated disconnected transport"),
            _ => Task.CompletedTask);
        Assert.True(runtime.Start([new PageDefinition { Line1Template = "PAGE" }]));
        await Assert.ThrowsAsync<IOException>(() =>
            runtime.TickAsync(DateTimeOffset.UnixEpoch, new Dictionary<string, object?>()));
        await runtime.StopAndDrainAsync();
        Assert.False(runtime.IsRunning);
        Assert.False(runtime.IsBusy);
    }
}
