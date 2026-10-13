using LIS2.App.Avalonia;
using LIS2.Core;

namespace LIS2.App.Avalonia.Tests;

public sealed class UnifiedDisplayFanArbitrationTests
{
    [Fact]
    public async Task DisplayFacadeUsesUnifiedSession()
    {
        var transport = new VirtualLis2Transport();
        await using var display = new SerialDisplaySession(transport);
        await display.ConnectAsync();
        await display.WriteLineAsync(1, "hello");
        await display.RenderBothAsync("one", "two");
        await display.SetBrightnessAsync(Lis2Brightness.Percent50);
        await display.ClearAsync();
        Assert.Equal(5, transport.Writes.Count);
        Assert.DoesNotContain(transport.Writes, write => write.SequenceEqual(Lis2Protocol.SetFans(100, 100, 100, 100)));
    }

    [Fact]
    public async Task DisplayAndFansShareOneCommandGate()
    {
        var transport = new VirtualLis2Transport();
        await using var session = new UnifiedLis2Session(transport);
        await session.ConnectWithFanSafetyAsync(new[] { 100, 100, 100, 100 });
        await Task.WhenAll(session.RenderBothAsync("line1", "line2"), session.WriteAsync(new[] { 55, 60, 65, 70 }, CancellationToken.None));
        Assert.Equal(4, transport.Writes.Count);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[0]);
        Assert.Equal(1, transport.Writes.Count(w => w.SequenceEqual(Lis2Protocol.SetFans(55, 60, 65, 70))));
    }
}
