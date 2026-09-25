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

    [Fact]
    public async Task VirtualTransport_TracksDisplayState()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);

        await device.ConnectAsync();
        await device.WriteLineAsync(1, "HELLO");
        await device.WriteAsync(2, 5, "WORLD");

        Assert.StartsWith("HELLO", transport.State.Line1);
        Assert.Equal("WORLD", transport.State.Line2.Substring(5, 5));
    }

    [Fact]
    public async Task VirtualTransport_TracksBrightnessAndFans()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);

        await device.ConnectAsync();
        await device.SetBrightnessAsync(Lis2Brightness.Percent50);
        await device.SetFansAsync(25, 50, 75, 100);

        Assert.Equal(Lis2Brightness.Percent50, transport.State.Brightness);
        Assert.Equal(25, transport.State.Fan1);
        Assert.Equal(50, transport.State.Fan2);
        Assert.Equal(75, transport.State.Fan3);
        Assert.Equal(100, transport.State.Fan4);
    }

    [Fact]
    public async Task VirtualTransport_TracksCustomCharacterRows()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);

        await device.ConnectAsync();
        await device.ProgramCharacterAsync(
            2,
            new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 });

        Assert.Equal(
            new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 },
            transport.State.CustomCharacters[1]);
    }
    [Fact]
    public async Task CustomCharacterManager_SkipsUnchangedGlyph()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);
        await device.ConnectAsync();

        var manager = new CustomCharacterManager(device);
        var rows = new byte[] { 0, 4, 14, 31, 14, 4, 0, 0 };

        await manager.ProgramSlotAsync(1, rows);
        await manager.ProgramSlotAsync(1, rows);

        Assert.Equal(8, transport.Writes.Count);
    }

    [Fact]
    public async Task CustomCharacterManager_ReprogramsChangedGlyph()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);
        await device.ConnectAsync();

        var manager = new CustomCharacterManager(device);

        await manager.ProgramSlotAsync(1, new byte[] { 0, 4, 14, 31, 14, 4, 0, 0 });
        await manager.ProgramSlotAsync(1, new byte[] { 0, 4, 14, 31, 4, 4, 0, 0 });

        Assert.Equal(16, transport.Writes.Count);
    }
    [Fact]
    public async Task VirtualTransport_DecodesNativeInternationalCharacters()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);

        await device.ConnectAsync();
        await device.WriteLineAsync(1, "ÄÖÜ äöü ß °");
        await device.WriteLineAsync(2, "← → Ω π Σ √ ∞");

        Assert.StartsWith("ÄÖÜ äöü ß °", transport.State.Line1);
        Assert.StartsWith("← → Ω π Σ √ ∞", transport.State.Line2);
    }

    [Fact]
    public async Task VirtualTransport_ReportsUnknownCommandsWithoutMutatingDisplay()
    {
        var transport = new VirtualLis2Transport();
        await transport.OpenAsync();

        await transport.WriteAsync(new byte[] { 0xFF, 0x01, 0x02 });

        Assert.Equal(1, transport.State.RejectedCommandCount);
        Assert.Contains("Unknown LIS2 command", transport.State.LastProtocolError);
        Assert.Equal(new string(' ', 20), transport.State.Line1);
        Assert.Equal(new string(' ', 20), transport.State.Line2);
    }

    [Fact]
    public async Task VirtualTransport_ReportsMalformedKnownCommands()
    {
        var transport = new VirtualLis2Transport();
        await transport.OpenAsync();

        await transport.WriteAsync(new byte[] { 0xA1, 0x00, 0x00 });

        Assert.Equal(1, transport.State.RejectedCommandCount);
        Assert.Contains("Malformed LIS2 display-write", transport.State.LastProtocolError);
    }

    [Fact]
    public async Task RawDisplayWrite_IsVisibleInVirtualTransport()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);

        await device.ConnectAsync();
        await device.WriteRawDisplayBytesAsync(
            1,
            0,
            new byte[] { 0x80, 0x86, 0x8A, 0xDF, 0xF4 });

        Assert.StartsWith("ÄÖÜ°Ω", transport.State.Line1);
    }
}
