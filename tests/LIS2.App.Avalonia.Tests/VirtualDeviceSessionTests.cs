using LIS2.App.Avalonia;
using LIS2.Core;

namespace LIS2.App.Avalonia.Tests;

public sealed class VirtualDeviceSessionTests
{
    [Fact]
    public async Task Demo_UpdatesBothDisplayLines_WithoutProtocolErrors()
    {
        await using var session = new VirtualDeviceSession();
        await session.ConnectAsync();
        await session.WriteDemoAsync();

        Assert.Equal("LIS2 Control Center", session.State.Line1.TrimEnd());
        Assert.Equal("Avalonia Windows/Lin", session.State.Line2);
        Assert.Equal(0, session.State.RejectedCommandCount);
        Assert.Equal(3, session.CommandCount);
    }

    [Fact]
    public async Task RenderBoth_UsesOneBasedProtocolLines()
    {
        await using var session = new VirtualDeviceSession();
        await session.ConnectAsync();
        await session.RenderBothAsync("first", "second");

        Assert.Equal("first", session.State.Line1.TrimEnd());
        Assert.Equal("second", session.State.Line2.TrimEnd());
        Assert.Equal(0, session.State.RejectedCommandCount);
    }

    [Fact]
    public async Task Brightness_UpdatesVirtualDevice()
    {
        await using var session = new VirtualDeviceSession();
        await session.ConnectAsync();
        await session.SetBrightnessAsync(Lis2Brightness.Percent25);

        Assert.Equal(Lis2Brightness.Percent25, session.State.Brightness);
        Assert.Equal(0, session.State.RejectedCommandCount);
    }

    [Fact]
    public async Task Clear_ResetsBothLines()
    {
        await using var session = new VirtualDeviceSession();
        await session.ConnectAsync();
        await session.RenderBothAsync("first", "second");
        await session.ClearAsync();

        Assert.Equal(new string(' ', 20), session.State.Line1);
        Assert.Equal(new string(' ', 20), session.State.Line2);
        Assert.Equal(0, session.State.RejectedCommandCount);
    }

    [Theory]
    [InlineData(0x20)]
    [InlineData(0x80)]
    [InlineData(0xD8)]
    public async Task RomRange_UpdatesBothLines_WithoutRejectedCommands(int start)
    {
        await using var session = new VirtualDeviceSession();
        await session.ConnectAsync();
        await session.SendCharacterRomRangeAsync(start);

        Assert.Equal(20, session.State.Line1.Length);
        Assert.Equal(20, session.State.Line2.Length);
        Assert.Equal(2, session.CommandCount);
        Assert.Equal(0, session.State.RejectedCommandCount);
    }

    [Theory]
    [InlineData(0x1F)]
    [InlineData(0xD9)]
    public async Task RomRange_RejectsInvalidStart(int start)
    {
        await using var session = new VirtualDeviceSession();
        await session.ConnectAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.SendCharacterRomRangeAsync(start));
        Assert.Equal(0, session.CommandCount);
    }

    [Fact]
    public async Task CustomGlyph_ProgramsEightRows_AndUpdatesVirtualState()
    {
        const string bitmap = "00100\n01110\n11111\n00100\n00100\n00100\n00000\n00000";
        var rows = GlyphBitmap.Parse(bitmap);
        Assert.Equal(bitmap, GlyphBitmap.Format(rows));

        await using var session = new VirtualDeviceSession();
        await session.ConnectAsync();
        await session.ProgramGlyphAsync(3, rows);

        Assert.Equal(rows, session.State.CustomCharacters[2]);
        Assert.Equal(8, session.CommandCount);
        Assert.Equal(0, session.State.RejectedCommandCount);
    }

    [Theory]
    [InlineData("00000")]
    [InlineData("00000\n00000\n00000\n00000\n00000\n00000\n00000\n00002")]
    public void GlyphBitmap_RejectsMalformedRows(string bitmap)
    {
        Assert.Throws<ArgumentException>(() => GlyphBitmap.Parse(bitmap));
    }

    [Fact]
    public async Task WritesRequireConnection()
    {
        await using var session = new VirtualDeviceSession();

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.RenderBothAsync("one", "two"));
        Assert.Equal(0, session.CommandCount);
    }

    [Fact]
    public async Task SpecialCharacterTest_UsesSupportedRomMapping()
    {
        await using var session = new VirtualDeviceSession();
        await session.ConnectAsync();
        await session.SendSpecialCharacterTestAsync();

        Assert.Equal(0, session.State.RejectedCommandCount);
        Assert.Equal(2, session.CommandCount);
        Assert.Contains("Ä", session.State.Line1);
    }
}
