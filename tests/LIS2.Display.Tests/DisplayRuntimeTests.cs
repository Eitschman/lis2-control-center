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

    [Fact]
    public void RenderNext_LongLineScrollsWithoutAdvancingPage()
    {
        var scheduler = new PageScheduler();
        scheduler.ReplacePages(new[]
        {
            new DisplayPage(
                "long",
                "Long",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "SECOND LINE",
                TimeSpan.FromSeconds(10)),
            new DisplayPage(
                "next",
                "Next",
                "NEXT PAGE",
                string.Empty,
                TimeSpan.FromSeconds(10))
        });

        var runtime = new DisplayRuntime(
            new TemplateRenderer(),
            scheduler,
            new EventQueue());

        var now = DateTimeOffset.UtcNow;

        var first = runtime.RenderNext(
            new Dictionary<string, object?>(),
            now);

        var scrolled = runtime.RenderNext(
            new Dictionary<string, object?>(),
            now.AddMilliseconds(900));

        Assert.NotNull(first);
        Assert.NotNull(scrolled);
        Assert.Equal("ABCDEFGHIJKLMNOPQRST", first.Line1);
        Assert.Equal("BCDEFGHIJKLMNOPQRSTU", scrolled.Line1);
        Assert.StartsWith("SECOND LINE", scrolled.Line2);
        Assert.True(runtime.SuggestedDuration <= PingPongScroller.StepInterval);
    }

    [Fact]
    public void RenderNext_SwitchesPageOnlyAfterPageDuration()
    {
        var scheduler = new PageScheduler();
        scheduler.ReplacePages(new[]
        {
            new DisplayPage("one", "One", "PAGE ONE", string.Empty, TimeSpan.FromSeconds(2)),
            new DisplayPage("two", "Two", "PAGE TWO", string.Empty, TimeSpan.FromSeconds(2))
        });

        var runtime = new DisplayRuntime(
            new TemplateRenderer(),
            scheduler,
            new EventQueue());

        var now = DateTimeOffset.UtcNow;

        var first = runtime.RenderNext(new Dictionary<string, object?>(), now);
        var stillFirst = runtime.RenderNext(
            new Dictionary<string, object?>(),
            now.AddSeconds(1));
        var second = runtime.RenderNext(
            new Dictionary<string, object?>(),
            now.AddSeconds(2));

        Assert.NotNull(first);
        Assert.NotNull(stillFirst);
        Assert.NotNull(second);
        Assert.StartsWith("PAGE ONE", first.Line1);
        Assert.StartsWith("PAGE ONE", stillFirst.Line1);
        Assert.StartsWith("PAGE TWO", second.Line1);
    }

    [Fact]
    public void RenderNext_SkipsPageWhenVisibilityDoesNotMatch()
    {
        var scheduler = new PageScheduler();
        scheduler.ReplacePages(new[]
        {
            new DisplayPage(
                "winamp",
                "Winamp",
                "{Winamp.Artist}",
                "{Winamp.Title}",
                TimeSpan.FromSeconds(5),
                VisibilityExpression: "Winamp.State=Playing"),
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
                ["Winamp.State"] = "Stopped",
                ["Clock.Time"] = "12:34",
                ["Clock.Date"] = "23.09.2026"
            },
            DateTimeOffset.UtcNow);

        Assert.NotNull(frame);
        Assert.StartsWith("12:34", frame.Line1);
    }
}
