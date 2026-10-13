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
}
