using LIS2.Display;

namespace LIS2.Display.Tests;

public sealed class DisplayRuntimeTests
{
    [Fact]
    public void RenderNext_RendersScheduledPage()
    {
        var scheduler = new PageScheduler();
        scheduler.ReplacePages(new[]
        {
            new DisplayPage(
                "clock",
                "Clock",
                "{Clock.Time}",
                "{Clock.Date}",
                TimeSpan.FromSeconds(5))
        });

        var runtime = new DisplayRuntime(
            new TemplateRenderer(),
            scheduler,
            new EventQueue());

        var frame = runtime.RenderNext(
            new Dictionary<string, object?>
            {
                ["Clock.Time"] = "12:34:56",
                ["Clock.Date"] = "23.09.2026"
            },
            DateTimeOffset.UtcNow);

        Assert.NotNull(frame);
        Assert.StartsWith("12:34:56", frame.Line1);
        Assert.StartsWith("23.09.2026", frame.Line2);
    }

    [Fact]
    public void RenderNext_EventOverridesScheduledPage()
    {
        var scheduler = new PageScheduler();
        scheduler.ReplacePages(new[]
        {
            new DisplayPage("normal", "Normal", "Normal", "Page", TimeSpan.FromSeconds(5))
        });

        var events = new EventQueue();
        var now = DateTimeOffset.UtcNow;
        events.Add(new DisplayEvent(
            "warning",
            DisplayFrame.Create("WARNING", "Something happened"),
            100,
            now.AddMinutes(1)));

        var runtime = new DisplayRuntime(
            new TemplateRenderer(),
            scheduler,
            events);

        var frame = runtime.RenderNext(
            new Dictionary<string, object?>(),
            now);

        Assert.NotNull(frame);
        Assert.StartsWith("WARNING", frame.Line1);
    }
}
