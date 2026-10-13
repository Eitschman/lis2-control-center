using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class SerialPortDiscoveryTests
{
    [Fact]
    public void EmptyInputProducesEmptyResult()
    {
        Assert.Empty(SerialPortDiscovery.PreferStableLinuxPaths(Array.Empty<string>(), Array.Empty<string>()));
    }

    [Fact]
    public void PortsAreDeduplicatedAndSorted()
    {
        var ports = SerialPortDiscovery.PreferStableLinuxPaths(
            new[] { "/dev/ttyUSB2", "/dev/ttyUSB0", "/dev/ttyUSB0" },
            Array.Empty<string>());
        Assert.Equal(new[] { "/dev/ttyUSB0", "/dev/ttyUSB2" }, ports);
    }

    [Fact]
    public void BrokenByIdLinkDoesNotHidePort()
    {
        var ports = SerialPortDiscovery.PreferStableLinuxPaths(
            new[] { "/dev/ttyUSB0" }, new[] { "/dev/serial/by-id/nonexistent-lis2-test" });
        Assert.Single(ports);
        Assert.Equal("/dev/ttyUSB0", ports[0]);
    }
}
