using LIS2.Core;
using LIS2.Display;

namespace LIS2.App.Tests;

public sealed class VirtualLis2EndToEndTests
{
    [Fact]
    public async Task RuntimeFrame_ReachesVirtualDisplayState()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);
        await device.ConnectAsync();

        var scheduler = new PageScheduler();
        scheduler.ReplacePages(
        [
            new DisplayPage(
                "ha",
                "HA",
                "CPU {HA.sensor.cpu|fallback:--|percent:0}",
                "{Winamp.Title|fallback:Nothing playing}",
                TimeSpan.FromSeconds(5))
        ]);

        var runtime = new DisplayRuntime(
            new TemplateRenderer(),
            scheduler,
            new EventQueue());

        var frame = runtime.RenderNext(
            new Dictionary<string, object?>
            {
                ["HA.sensor.cpu"] = 42.4,
                ["Winamp.Title"] = "Turrican"
            },
            DateTimeOffset.UtcNow);

        Assert.NotNull(frame);

        var writer = new DisplayFrameWriter(device);
        await writer.WriteAsync(frame!);

        Assert.StartsWith("CPU 42%", transport.State.Line1);
        Assert.StartsWith("Turrican", transport.State.Line2);
    }

    [Fact]
    public async Task EventOverlay_ReplacesPageAndThenReturnsToPage()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);
        await device.ConnectAsync();

        var scheduler = new PageScheduler();
        scheduler.ReplacePages(
        [
            new DisplayPage("normal", "Normal", "NORMAL", "PAGE", TimeSpan.FromSeconds(30))
        ]);

        var now = DateTimeOffset.UtcNow;
        var events = new EventQueue();
        var runtime = new DisplayRuntime(new TemplateRenderer(), scheduler, events);
        var writer = new DisplayFrameWriter(device);

        await writer.WriteAsync(runtime.RenderNext(new Dictionary<string, object?>(), now)!);
        Assert.StartsWith("NORMAL", transport.State.Line1);

        events.Add(new DisplayEvent(
            "test",
            DisplayFrame.Create("ALERT", "ACTIVE"),
            100,
            now.AddSeconds(2)));

        await writer.WriteAsync(runtime.RenderNext(
            new Dictionary<string, object?>(),
            now.AddSeconds(1))!);
        Assert.StartsWith("ALERT", transport.State.Line1);

        await writer.WriteAsync(runtime.RenderNext(
            new Dictionary<string, object?>(),
            now.AddSeconds(3))!);
        Assert.StartsWith("NORMAL", transport.State.Line1);
    }

    [Fact]
    public async Task VirtualDevice_TracksBrightnessFansGlyphsAndDisplayTogether()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);
        await device.ConnectAsync();

        await device.SetBrightnessAsync(Lis2Brightness.Percent75);
        await device.SetFansAsync(30, 45, 60, 100);
        await device.ProgramCharacterAsync(
            1,
            new byte[] { 0, 4, 14, 31, 14, 4, 0, 0 });
        await device.WriteLineAsync(1, "SYSTEM READY");
        await device.WriteLineAsync(2, "VIRTUAL LIS2");

        Assert.Equal(Lis2Brightness.Percent75, transport.State.Brightness);
        Assert.Equal(30, transport.State.Fan1);
        Assert.Equal(45, transport.State.Fan2);
        Assert.Equal(60, transport.State.Fan3);
        Assert.Equal(100, transport.State.Fan4);
        Assert.Equal(new byte[] { 0, 4, 14, 31, 14, 4, 0, 0 }, transport.State.CustomCharacters[0]);
        Assert.StartsWith("SYSTEM READY", transport.State.Line1);
        Assert.StartsWith("VIRTUAL LIS2", transport.State.Line2);
    }

    [Fact]
    public async Task DisconnectedVirtualTransport_RejectsWrites()
    {
        var transport = new VirtualLis2Transport();
        await using var device = new Lis2Device(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => device.WriteLineAsync(1, "NO CONNECTION"));
    }
}