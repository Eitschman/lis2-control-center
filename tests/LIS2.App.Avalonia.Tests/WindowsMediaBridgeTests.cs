using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests
{
    public sealed class WindowsMediaBridgeTests
    {
        [Fact]
        public async Task StartupFailureCleansUpAndSnapshotFailureIsVisible()
        {
            if (!OperatingSystem.IsWindows()) return;
            var provider = typeof(LIS2.WindowsMedia.WindowsMediaDataSource);
            LIS2.WindowsMedia.WindowsMediaDataSource.FailStart = true;
            await using var bridge = new WindowsMediaBridge();
            await Assert.ThrowsAsync<IOException>(() => bridge.StartAsync(provider.Assembly.Location));
            Assert.False(bridge.IsAvailable);
            Assert.Equal(1, LIS2.WindowsMedia.WindowsMediaDataSource.Disposals);
            LIS2.WindowsMedia.WindowsMediaDataSource.FailStart = false;
            await bridge.StartAsync(provider.Assembly.Location);
            Assert.True(bridge.IsAvailable);
            await Assert.ThrowsAsync<InvalidOperationException>(() => bridge.StartAsync(provider.Assembly.Location));
            var failure = Assert.Throws<System.Reflection.TargetInvocationException>(() => bridge.Values);
            Assert.IsType<IOException>(failure.InnerException);
            await bridge.DisposeAsync();
            Assert.False(bridge.IsAvailable);
            Assert.Equal(false, bridge.Values["Connected"]);
            await Assert.ThrowsAsync<FileNotFoundException>(() => bridge.StartAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll")));
        }
    }
}

namespace LIS2.WindowsMedia
{
    // Der Testanbieter bildet den Reflection-Vertrag ab, ohne eine echte Windows-Mediensitzung zu benötigen.
    public sealed class WindowsMediaDataSource(object? clock)
    {
        public static bool FailStart { get; set; }
        public static int Disposals { get; private set; }
        public IReadOnlyDictionary<string, object?> Values => throw new IOException("snapshot failed");
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = clock;
            return FailStart ? Task.FromException(new IOException("startup failed")) : Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public ValueTask DisposeAsync() { Disposals++; return ValueTask.CompletedTask; }
    }
}
