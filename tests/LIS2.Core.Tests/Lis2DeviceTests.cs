using LIS2.Core;

namespace LIS2.Core.Tests;

public sealed class Lis2DeviceTests
{
    [Fact]
    public async Task VirtualTransport_ReceivesDeviceWrites()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);

        await device.ConnectAsync();
        await device.SetFansAsync(50, 50, 75, 100);

        Assert.Single(transport.Writes);
        Assert.Equal(new byte[] { 0xAE, 0xF0, 0x32, 0x32, 0x4B, 0x64 }, transport.Writes[0]);
    }

    [Fact]
    public async Task ProgramCharacter_WritesEightPackets()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);

        await device.ConnectAsync();
        await device.ProgramCharacterAsync(
            1,
            new byte[] { 0x00, 0x04, 0x0E, 0x1F, 0x0E, 0x04, 0x00, 0x00 });

        Assert.Equal(8, transport.Writes.Count);
        Assert.Equal(new byte[] { 0xAB, 0x01, 0x00, 0x00 }, transport.Writes[0]);
        Assert.Equal(new byte[] { 0xAB, 0x01, 0x07, 0x00 }, transport.Writes[7]);
    }
}
