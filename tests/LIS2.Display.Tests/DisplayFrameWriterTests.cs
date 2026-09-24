using LIS2.Core;
using LIS2.Display;

namespace LIS2.Display.Tests;

public sealed class DisplayFrameWriterTests
{
    [Fact]
    public async Task FirstFrame_WritesBothFullLines()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);
        await device.ConnectAsync();

        var writer = new DisplayFrameWriter(device);
        await writer.WriteAsync(DisplayFrame.Create("HELLO", "WORLD"));

        Assert.Equal(2, transport.Writes.Count);
        Assert.Equal(0xA1, transport.Writes[0][0]);
        Assert.Equal(0xA2, transport.Writes[1][0]);
        Assert.Equal(20, transport.Writes[0].Length - 3);
        Assert.Equal(20, transport.Writes[1].Length - 3);
    }

    [Fact]
    public async Task IdenticalFrame_WritesNothing()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);
        await device.ConnectAsync();

        var writer = new DisplayFrameWriter(device);
        var frame = DisplayFrame.Create("AAAA", "BBBB");

        await writer.WriteAsync(frame);
        var writesAfterFirstFrame = transport.Writes.Count;

        await writer.WriteAsync(frame);

        Assert.Equal(writesAfterFirstFrame, transport.Writes.Count);
    }

    [Fact]
    public async Task ChangedCharacter_WritesOnlyChangedSpan()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);
        await device.ConnectAsync();

        var writer = new DisplayFrameWriter(device);

        await writer.WriteAsync(DisplayFrame.Create("AAAA", "BBBB"));
        await writer.WriteAsync(DisplayFrame.Create("AABA", "BBBB"));

        Assert.Equal(3, transport.Writes.Count);
        Assert.Equal(
            new byte[] { 0xA1, 0x02, 0xA7, 0x42 },
            transport.Writes[2]);
    }

    [Fact]
    public async Task Reset_ForcesFullRewrite()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);
        await device.ConnectAsync();

        var writer = new DisplayFrameWriter(device);
        var frame = DisplayFrame.Create("HELLO", "WORLD");

        await writer.WriteAsync(frame);
        writer.Reset();
        await writer.WriteAsync(frame);

        Assert.Equal(4, transport.Writes.Count);
    }
    [Fact]
    public async Task CustomGlyphCell_WritesRawSlotByte()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);
        await device.ConnectAsync();

        var writer = new DisplayFrameWriter(device);
        var glyph = Lis2Protocol.CustomGlyph(4);

        await writer.WriteAsync(DisplayFrame.Create($"A{glyph}B", string.Empty));

        Assert.Equal(0x04, transport.Writes[0][4]);
    }
}
