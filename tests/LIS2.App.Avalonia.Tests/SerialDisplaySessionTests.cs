using LIS2.App.Avalonia;
using LIS2.Core;
using LIS2.Runtime;
using LIS2.Fans;

namespace LIS2.App.Avalonia.Tests;

public sealed class SerialDisplaySessionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task FailedManualWriteRevokesAuthorizationAndRestoresSafeOutput(int failAt)
    {
        var transport = new FailingManualTransport(failAt);
        await using var session = new SerialDisplaySession(transport);
        await session.ConnectAsync();
        await Assert.ThrowsAsync<IOException>(() => session.ApplyManualFansAsync(new[] { 40, 40, 40, 40 }, true));
        Assert.False(session.FansRunning);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Inner.Writes[^1]);
        await session.DisconnectAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.ApplyManualFansAsync(new[] { 40, 40, 40, 40 }, true));
    }

    private sealed class FailingManualTransport(int failAt) : ILis2Transport
    {
        public VirtualLis2Transport Inner { get; } = new();
        public bool IsOpen => Inner.IsOpen;
        public Task OpenAsync(CancellationToken cancellationToken = default) => Inner.OpenAsync(cancellationToken);
        public Task CloseAsync(CancellationToken cancellationToken = default) => Inner.CloseAsync(cancellationToken);
        public async Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            await Inner.WriteAsync(data, cancellationToken);
            // Auch ein nach angenommener Ausgabe gemeldeter Fehler erfordert die Sicherheitsausgabe.
            if (Inner.Writes.Count == failAt) throw new IOException("Simulierter Schreibfehler.");
        }
        public ValueTask DisposeAsync() => Inner.DisposeAsync();
    }

    [Fact]
    public async Task ManualFansRejectMissingConsentAndInvalidOutputsBeforeAnyWrite()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new SerialDisplaySession(transport);
        await session.ConnectAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.ApplyManualFansAsync(new[] { 40, 40, 40, 40 }, false));
        await Assert.ThrowsAsync<ArgumentException>(() => session.ApplyManualFansAsync(new[] { -1, 40, 40, 40 }, true));
        await Assert.ThrowsAsync<ArgumentException>(() => session.ApplyManualFansAsync(new[] { 101, 40, 40, 40 }, true));
        await Assert.ThrowsAsync<ArgumentException>(() => session.ApplyManualFansAsync(new[] { 40 }, true));
        Assert.Empty(transport.Writes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ManualFansWriteSafeFrameFirstAndRestoreOnStopOrDisconnect(bool disconnect)
    {
        var transport = new VirtualLis2Transport();
        await using var session = new SerialDisplaySession(transport);
        await session.ConnectAsync();
        await session.ApplyManualFansAsync(new[] { 40, 50, 60, 70 }, true);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[0]);
        Assert.Equal(Lis2Protocol.SetFans(40, 50, 60, 70), transport.Writes[1]);
        Assert.True(session.FansRunning);
        Assert.False(session.AutomaticFansRunning);
        await session.ApplyManualFansAsync(new[] { 45, 55, 65, 75 }, true);
        Assert.Equal(3, transport.Writes.Count);
        if (disconnect) await session.DisconnectAsync();
        else await session.StopFansAsync();
        Assert.False(session.FansRunning);
        Assert.Equal(!disconnect, session.IsConnected);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[^1]);
        if (!disconnect)
        {
            await session.ApplyManualFansAsync(new[] { 40, 40, 40, 40 }, true);
            Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[^2]);
        }
    }

    [Fact]
    public async Task ManualAndAutomaticFansCannotOwnTheSameSession()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new SerialDisplaySession(transport);
        await session.ConnectAsync();
        var channels = new PortableFanSettings().ToRuntimeChannels();
        await session.StartFansAsync(new SimulatedFanSensors(), channels);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.ApplyManualFansAsync(new[] { 40, 40, 40, 40 }, true));
        await session.StopFansAsync();
        await session.ApplyManualFansAsync(new[] { 40, 40, 40, 40 }, true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.StartFansAsync(new SimulatedFanSensors(), channels));
    }

    [Fact]
    public async Task FanControlSharesDisplayPortAndCanStopWithoutDisconnecting()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new SerialDisplaySession(transport);
        await session.ConnectAsync();
        await session.RenderBothAsync("before", "fans");
        var sensors = new SimulatedFanSensors();
        var channels = Enumerable.Range(0, 4).Select(_ => new FanChannelConfiguration
        {
            Mode = FanMode.Fixed, FixedPercent = 55, MinimumPercent = 30,
            MaximumPercent = 100, FailSafePercent = 100
        }).ToArray();
        await session.StartFansAsync(sensors, channels);
        Assert.True(session.FansRunning);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[2]);
        Assert.Equal(Lis2Protocol.SetFans(55, 55, 55, 55), transport.Writes[3]);
        await session.RenderBothAsync("during", "fans");
        await session.StopFansAsync();
        Assert.False(session.FansRunning);
        Assert.True(session.IsConnected);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[^1]);
        await session.RenderBothAsync("after", "fans");
        Assert.StartsWith("after", transport.State.Line1);
    }

    [Fact]
    public async Task DisconnectDrainsActiveFansAndRestoresSafeFrame()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new SerialDisplaySession(transport);
        await session.ConnectAsync();
        var channels = Enumerable.Range(0, 4).Select(_ => new FanChannelConfiguration
        {
            Mode = FanMode.Fixed, FixedPercent = 60, MinimumPercent = 30,
            MaximumPercent = 100, FailSafePercent = 100
        }).ToArray();
        await session.StartFansAsync(new SimulatedFanSensors(), channels);
        await session.DisconnectAsync();
        Assert.False(session.IsConnected);
        Assert.False(session.FansRunning);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[^1]);
    }

    [Fact]
    public async Task DisplayOnlyCommandsNeverWriteFanProtocol()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new SerialDisplaySession(transport);
        await session.ConnectAsync();
        await session.RenderBothAsync("Line one", "Line two");
        Assert.Equal("Line one", transport.State.Line1.TrimEnd());
        await session.SetBrightnessAsync(Lis2Brightness.Percent50);
        await session.ClearAsync();
        Assert.NotEmpty(transport.Writes);
        Assert.DoesNotContain(transport.Writes, w => w.SequenceEqual(Lis2Protocol.SetFans(0, 0, 0, 0)));
    }

    [Fact]
    public async Task GlyphProgrammingRequiresEightValidGlyphsBeforeAnyWrites()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new SerialDisplaySession(transport);
        await session.ConnectAsync();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            session.ProgramAllGlyphsAsync(new List<CustomGlyphSettings>()));
        Assert.Empty(transport.Writes);
    }

    [Fact]
    public async Task ConnectAndDisconnectAreExplicit()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new SerialDisplaySession(transport);
        Assert.False(session.IsConnected);
        await session.ConnectAsync();
        Assert.True(session.IsConnected);
        await session.DisconnectAsync();
        Assert.False(session.IsConnected);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.WriteLineAsync(1, "disconnected"));
    }
    [Fact]
    public async Task ConcurrentStartAndDisconnectNeverAuthorizeAfterDisconnect()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new SerialDisplaySession(transport);
        await session.ConnectAsync();
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sensors = new BlockingSensors(entered, release);
        var channels = Enumerable.Range(0, 4).Select(_ => new FanChannelConfiguration
        {
            Mode = FanMode.Curve, SensorKey = "cpu", MinimumPercent = 30,
            MaximumPercent = 100, FailSafePercent = 100
        }).ToArray();
        var start = Task.Run(() => session.StartFansAsync(sensors, channels));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var disconnect = session.DisconnectAsync();
        Assert.False(disconnect.IsCompleted);
        release.TrySetResult(true);
        await start;
        await disconnect;
        Assert.False(session.IsConnected);
        Assert.False(session.FansRunning);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[^1]);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.StartFansAsync(new SimulatedFanSensors(), channels));
    }

    [Fact]
    public async Task DisposeDuringBlockedStartDrainsAndRejectsFurtherAuthorization()
    {
        var transport = new VirtualLis2Transport();
        var session = new SerialDisplaySession(transport);
        await session.ConnectAsync();
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var channels = Enumerable.Range(0, 4).Select(_ => new FanChannelConfiguration
        {
            Mode = FanMode.Curve, SensorKey = "cpu", MinimumPercent = 30,
            MaximumPercent = 100, FailSafePercent = 100
        }).ToArray();
        var start = Task.Run(() => session.StartFansAsync(new BlockingSensors(entered, release), channels));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var dispose = session.DisposeAsync().AsTask();
        Assert.False(dispose.IsCompleted);
        release.TrySetResult(true);
        await start;
        await dispose;
        Assert.False(session.IsConnected);
        Assert.False(session.FansRunning);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[^1]);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ConnectAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            session.StartFansAsync(new SimulatedFanSensors(), channels));
        await session.DisposeAsync();
    }

    private sealed class BlockingSensors : IFanSensorSource
    {
        private readonly TaskCompletionSource<bool> _entered;
        private readonly TaskCompletionSource<bool> _release;
        public BlockingSensors(TaskCompletionSource<bool> entered, TaskCompletionSource<bool> release)
        {
            _entered = entered;
            _release = release;
        }
        public IReadOnlyDictionary<string, object?> Snapshot()
        {
            _entered.TrySetResult(true);
            _release.Task.GetAwaiter().GetResult();
            return new Dictionary<string, object?> { ["cpu"] = 40d };
        }
    }

}
