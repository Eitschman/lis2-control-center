using LIS2.App.Avalonia;
using LIS2.Core;

namespace LIS2.App.Avalonia.Tests;

public sealed class SerialDisplaySessionTests
{
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
        Assert.False(transport.Writes.Any(w => w.SequenceEqual(Lis2Protocol.SetFans(0, 0, 0, 0))));
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
