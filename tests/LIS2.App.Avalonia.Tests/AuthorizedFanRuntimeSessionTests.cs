using LIS2.App.Avalonia;
using LIS2.Core;
using LIS2.Fans;
using LIS2.Runtime;

namespace LIS2.App.Avalonia.Tests;

public sealed class AuthorizedFanRuntimeSessionTests
{
    [Fact]
    public async Task StartsWithSafetyFrameAndRestoresItAfterRegulation()
    {
        var transport = new VirtualLis2Transport();
        var sensors = new SimulatedFanSensors();
        var channels = Enumerable.Range(0, 4).Select(_ => new FanChannelConfiguration
        {
            Mode = FanMode.Fixed, FixedPercent = 50, MinimumPercent = 30,
            MaximumPercent = 100, FailSafePercent = 100
        }).ToArray();
        await using var session = new AuthorizedFanRuntimeSession(
            transport, sensors, channels, TimeSpan.FromHours(1));
        await session.StartAsync(new[] { 100, 100, 100, 100 });
        Assert.True(session.FansAuthorized);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[0]);
        Assert.Equal(Lis2Protocol.SetFans(50, 50, 50, 50), transport.Writes[1]);
        await session.StopAsync();
        Assert.False(session.FansAuthorized);
        Assert.False(transport.IsOpen);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[^1]);
    }
}
