using Avalonia.Headless.XUnit;
using LIS2.App.Avalonia;
using LIS2.Core;
using LIS2.Runtime;

namespace LIS2.App.Avalonia.Tests;

public sealed class RuntimeHardeningTests
{
    [Fact]
    public async Task ShutdownDrainsDelayedStartAndRunsEveryCleanupAfterFailure()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lifetime = new AsyncProviderLifetime();
        var startup = lifetime.StartAsync(() => started.Task);
        var disposed = false;
        var shutdown = lifetime.StopAsync(() => { disposed = true; return Task.CompletedTask; });
        Assert.False(shutdown.IsCompleted);
        Assert.False(disposed);
        await Assert.ThrowsAsync<InvalidOperationException>(() => lifetime.StartAsync(() => Task.CompletedTask));
        started.SetException(new IOException("delayed startup failed"));
        await Assert.ThrowsAsync<IOException>(() => startup);
        var error = await Assert.ThrowsAsync<AggregateException>(() => shutdown);
        Assert.Single(error.InnerExceptions);
        Assert.True(disposed);
        Assert.Same(shutdown, lifetime.StopAsync(() => throw new InvalidOperationException()));

        var finalStep = false;
        await Assert.ThrowsAsync<AggregateException>(() => RuntimeShutdown.RunAsync(
            () => throw new IOException("first cleanup failed"),
            () => { finalStep = true; return Task.CompletedTask; }));
        Assert.True(finalStep);
    }

    [Fact]
    public async Task SuccessfulDelayedStartIsDisposedOnlyAfterCompletion()
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lifetime = new AsyncProviderLifetime();
        _ = lifetime.StartAsync(() => start.Task);
        var disposed = false;
        var stop = lifetime.StopAsync(() => { disposed = true; return Task.CompletedTask; });
        Assert.False(disposed);
        start.SetResult();
        await stop;
        Assert.True(disposed);
    }

    [Fact]
    public async Task SerialFramesWriteOnlyChangedSpansAndRecoverAfterPartialFailure()
    {
        var transport = new FailingTransport();
        await using var session = new SerialDisplaySession(transport);
        await session.ConnectAsync();
        await session.RenderBothAsync("CPU 20%", "Fan 50%");
        Assert.Equal(2, transport.Inner.Writes.Count);
        await session.RenderBothAsync("CPU 21%", "Fan 50%");
        Assert.Equal(3, transport.Inner.Writes.Count);
        Assert.Equal(Lis2Protocol.WriteLine(1, 5, "1"), transport.Inner.Writes[^1]);
        await session.RenderBothAsync("CPU 21%", "Fan 50%");
        Assert.Equal(3, transport.Inner.Writes.Count);
        transport.FailAfterWrite = true;
        await Assert.ThrowsAsync<IOException>(() => session.RenderBothAsync("CPU 22%", "Fan 51%"));
        await session.RenderBothAsync("CPU 21%", "Fan 50%");
        Assert.StartsWith("CPU 21%", transport.Inner.State.Line1);
        Assert.StartsWith("Fan 50%", transport.Inner.State.Line2);
        Assert.Contains("ERROR Write", session.Journal.Snapshot());
        await session.WriteRawDisplayBytesAsync(1, 0, [(byte)'X']);
        await session.RenderBothAsync("CPU 21%", "Fan 50%");
        Assert.StartsWith("CPU 21%", transport.Inner.State.Line1);
        await session.ClearAsync();
        await session.RenderBothAsync("CPU 21%", "Fan 50%");
        Assert.StartsWith("Fan 50%", transport.Inner.State.Line2);
    }

    [Fact]
    public async Task VirtualFramesInvalidateOnManualWriteClearAndReconnect()
    {
        await using var session = new VirtualDeviceSession();
        await session.ConnectAsync();
        await session.RenderBothAsync("ABC", "DEF");
        await session.RenderBothAsync("AXC", "DEF");
        Assert.Equal(3, session.CommandCount);
        Assert.Equal(Lis2Protocol.WriteLine(1, 1, "X"), session.CommandHistory[^1]);
        await session.WriteLineAsync(1, "manual");
        await session.RenderBothAsync("AXC", "DEF");
        Assert.StartsWith("AXC", session.State.Line1);
        await session.ClearAsync();
        await session.RenderBothAsync("AXC", "DEF");
        Assert.StartsWith("DEF", session.State.Line2);
        await session.DisconnectAsync();
        await session.ConnectAsync();
        var count = session.CommandCount;
        await session.RenderBothAsync("AXC", "DEF");
        Assert.Equal(count + 2, session.CommandCount);
    }

    [Fact]
    public void JournalRetainsBoundedRecentEntriesWithTimestamps()
    {
        var journal = new DiagnosticJournal(2);
        journal.Record("Test", "old entry");
        journal.Record("Test", "retained entry");
        journal.Record("Test", "latest entry");
        var lines = journal.Snapshot().Split(Environment.NewLine);
        Assert.Equal(2, lines.Length);
        Assert.DoesNotContain("old entry", journal.Snapshot());
        Assert.True(DateTimeOffset.TryParse(lines[0].Split(' ')[0], out _));
    }

    [Fact]
    public async Task ReconnectingUnifiedFanSessionSendsSafetyAndFullFrameAgain()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new UnifiedLis2Session(transport);
        await session.ConnectWithFanSafetyAsync([100, 100, 100, 100]);
        await session.RenderBothAsync("same", "frame");
        await session.DisconnectAsync();
        var count = transport.Writes.Count;
        await session.ConnectWithFanSafetyAsync([100, 100, 100, 100]);
        await session.RenderBothAsync("same", "frame");
        Assert.Equal(count + 3, transport.Writes.Count);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[count]);
    }

    [AvaloniaFact]
    public async Task AllViewsDialogsDiagnosticsAndMissingProviderUseActualControls()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-runtime-" + Guid.NewGuid().ToString("N"));
        var window = DesktopSmoke.CreateWindow(directory);
        try
        {
            window.Show();
            await DesktopSmoke.RunAsync(window, directory, crossProcessActivation: false);
            Assert.True(window.ShutdownCompletion.IsCompletedSuccessfully);
            var report = File.ReadAllText(Path.Combine(directory, "diagnostics.txt"));
            Assert.Contains("OK Write", report);
            Assert.Contains("startup failed", report);
            Assert.DoesNotContain("not yet available", report);
            var screenshotDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../TestResults/screenshots"));
            Directory.CreateDirectory(screenshotDirectory);
            foreach (var file in Directory.EnumerateFiles(directory, "*.png"))
                File.Copy(file, Path.Combine(screenshotDirectory, "port03-" + Path.GetFileName(file)), true);
        }
        finally
        {
            await window.BeginShutdownAsync();
            window.Close();
            Directory.Delete(directory, true);
        }
    }

    private sealed class FailingTransport : ILis2Transport
    {
        public VirtualLis2Transport Inner { get; } = new();
        public bool FailAfterWrite { get; set; }
        public bool IsOpen => Inner.IsOpen;
        public Task OpenAsync(CancellationToken token) => Inner.OpenAsync(token);
        public Task CloseAsync(CancellationToken token) => Inner.CloseAsync(token);
        public async Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken token)
        {
            await Inner.WriteAsync(data, token);
            if (FailAfterWrite) { FailAfterWrite = false; throw new IOException("accepted partial frame"); }
        }
        public ValueTask DisposeAsync() => Inner.DisposeAsync();
    }
}
