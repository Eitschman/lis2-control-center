using LIS2.App.Avalonia;
using LIS2.Core;

namespace LIS2.App.Avalonia.Tests;

public sealed class UnifiedLis2SessionTests
{
    [Fact]
    public async Task SafetyFramePrecedesDisplayAndRegulationCommands()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new UnifiedLis2Session(transport);
        await session.ConnectWithFanSafetyAsync(new[] { 100, 100, 100, 100 });
        await session.RenderBothAsync("first", "second");
        await session.WriteAsync(new[] { 50, 60, 70, 80 }, CancellationToken.None);
        Assert.Equal(4, transport.Writes.Count);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[0]);
        Assert.Equal(Lis2Protocol.SetFans(50, 60, 70, 80), transport.Writes[3]);
        await session.DisconnectAsync();
        Assert.False(session.FansAuthorized);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.WriteAsync(new[] { 50, 60, 70, 80 }, CancellationToken.None));
    }

    [Fact]
    public async Task AuthorizedDisconnectRestoresSafetyFrameBeforeClosing()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new UnifiedLis2Session(transport);
        await session.ConnectWithFanSafetyAsync(new[] { 100, 90, 80, 70 });
        await session.WriteAsync(new[] { 30, 40, 50, 60 }, CancellationToken.None);
        await session.DisconnectAsync();
        Assert.False(transport.IsOpen);
        Assert.False(session.FansAuthorized);
        Assert.Equal(Lis2Protocol.SetFans(100, 90, 80, 70), transport.Writes[^1]);
    }

    [Fact]
    public async Task AuthorizedDisposalRestoresSafetyFrame()
    {
        var transport = new VirtualLis2Transport();
        var session = new UnifiedLis2Session(transport);
        await session.ConnectWithFanSafetyAsync(new[] { 100, 100, 100, 100 });
        await session.WriteAsync(new[] { 25, 25, 25, 25 }, CancellationToken.None);
        await session.DisposeAsync();
        Assert.False(transport.IsOpen);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[^1]);
    }

    [Fact]
    public async Task RevokedAuthorizationStillRestoresSafetyOnDisposal()
    {
        var transport = new FailOnceFanTransport();
        var session = new UnifiedLis2Session(transport);
        await session.ConnectWithFanSafetyAsync(new[] { 100, 95, 90, 85 });
        transport.FailNextWrite = true;
        await Assert.ThrowsAsync<IOException>(() =>
            session.WriteAsync(new[] { 40, 40, 40, 40 }, CancellationToken.None));
        Assert.False(session.FansAuthorized);
        await session.DisposeAsync();
        Assert.False(transport.IsOpen);
        Assert.Equal(Lis2Protocol.SetFans(100, 95, 90, 85), transport.Writes[^1]);
    }

    [Fact]
    public async Task DisplayOnlyNeverAuthorizesFans()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new UnifiedLis2Session(transport);
        await session.ConnectDisplayOnlyAsync();
        await session.RenderBothAsync("one", "two");
        Assert.False(session.FansAuthorized);
        Assert.Equal(2, transport.Writes.Count);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.WriteAsync(new[] { 100, 100, 100, 100 }, CancellationToken.None));
    }

    [Fact]
    public async Task RejectsInvalidSafetyFrameBeforeConnecting()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new UnifiedLis2Session(transport);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            session.ConnectWithFanSafetyAsync(new[] { 100, 100, -1, 100 }));
        Assert.False(transport.IsOpen);
    }
    [Fact]
    public async Task FailedFanWriteRevokesAuthorizationButShutdownStillSendsSafetyFrame()
    {
        var transport = new FailOnceFanTransport();
        await using var session = new UnifiedLis2Session(transport);
        var safety = new[] { 100, 90, 80, 70 };
        await session.ConnectWithFanSafetyAsync(safety);
        transport.FailNextWrite = true;
        await Assert.ThrowsAsync<IOException>(() =>
            session.WriteAsync(new[] { 40, 40, 40, 40 }, CancellationToken.None));
        Assert.False(session.FansAuthorized);
        await session.DisconnectAsync();
        Assert.False(transport.IsOpen);
        Assert.Equal(Lis2Protocol.SetFans(100, 90, 80, 70), transport.Writes[^1]);
    }

    [Fact]
    public async Task FailedFanWriteStillRestoresSafetyFrameDuringDisposal()
    {
        var transport = new FailOnceFanTransport();
        var session = new UnifiedLis2Session(transport);
        await session.ConnectWithFanSafetyAsync(new[] { 100, 100, 100, 100 });
        transport.FailNextWrite = true;
        await Assert.ThrowsAsync<IOException>(() =>
            session.WriteAsync(new[] { 40, 40, 40, 40 }, CancellationToken.None));
        await session.DisposeAsync();
        Assert.False(transport.IsOpen);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[^1]);
    }

    private sealed class FailOnceFanTransport : ILis2Transport
    {
        private readonly VirtualLis2Transport _inner = new();
        public bool FailNextWrite { get; set; }
        public bool IsOpen => _inner.IsOpen;
        public IReadOnlyList<byte[]> Writes => _inner.Writes;
        public Task OpenAsync(CancellationToken cancellationToken = default) =>
            _inner.OpenAsync(cancellationToken);
        public Task CloseAsync(CancellationToken cancellationToken = default) =>
            _inner.CloseAsync(cancellationToken);
        public Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            if (FailNextWrite)
            {
                FailNextWrite = false;
                throw new IOException("Simulated transient serial write failure.");
            }
            return _inner.WriteAsync(data, cancellationToken);
        }
        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }
}
