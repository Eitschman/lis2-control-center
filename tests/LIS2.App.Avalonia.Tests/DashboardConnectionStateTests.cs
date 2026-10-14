using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class DashboardConnectionStateTests
{
    [Fact]
    public void DisconnectedDashboardExplicitlyOffersHardwareFreePreview()
    {
        Assert.Equal("Disconnected", DashboardConnectionState.Badge(false));
        Assert.Contains("without hardware", DashboardConnectionState.Description(false, 0));
    }

    [Fact]
    public void ConnectedDashboardReportsVirtualTransportAndCommandCount()
    {
        Assert.Equal("Connected (Virtual)", DashboardConnectionState.Badge(true));
        Assert.Equal("Virtual LIS2 connected. Commands: 12.", DashboardConnectionState.Description(true, 12));
    }
}
