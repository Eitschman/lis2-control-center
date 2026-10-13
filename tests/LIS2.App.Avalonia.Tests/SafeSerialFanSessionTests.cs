using LIS2.App.Avalonia;
using LIS2.Core;

namespace LIS2.App.Avalonia.Tests;

public sealed class SafeSerialFanSessionTests
{
    [Fact]
    public async Task SafeFrameIsFirstWriteAndRequiresExplicitAuthorization()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new SafeSerialFanSession(transport);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.WriteAsync(new[] { 50, 50, 50, 50 }, CancellationToken.None));
        Assert.Empty(transport.Writes);
        await session.ConnectAndAuthorizeAsync(new[] { 100, 100, 100, 100 });
        Assert.True(session.IsAuthorized);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[0]);
        await session.WriteAsync(new[] { 60, 70, 80, 90 }, CancellationToken.None);
        Assert.Equal(Lis2Protocol.SetFans(60, 70, 80, 90), transport.Writes[1]);
        await session.DisconnectAsync();
        Assert.False(session.IsAuthorized);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.WriteAsync(new[] { 60, 70, 80, 90 }, CancellationToken.None));
    }

    [Fact]
    public async Task InvalidSafetyFrameCannotOpenTransport()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new SafeSerialFanSession(transport);
        await Assert.ThrowsAsync<ArgumentException>(() => session.ConnectAndAuthorizeAsync(new[] { 100, 100, 101, 100 }));
        Assert.False(transport.IsOpen);
    }
}
