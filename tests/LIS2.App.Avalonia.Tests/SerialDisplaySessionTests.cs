using LIS2.App.Avalonia;
using LIS2.Core;
using LIS2.Runtime;
using LIS2.Fans;

namespace LIS2.App.Avalonia.Tests;

public sealed class SerialDisplaySessionTests
{
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
        Assert.Equal("after", transport.State.Line1.TrimEnd());
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
}
