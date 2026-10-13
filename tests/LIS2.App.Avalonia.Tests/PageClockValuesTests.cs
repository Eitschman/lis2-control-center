using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageClockValuesTests
{
    [Fact]
    public void ProvidesClockVariablesForTemplateRenderer()
    {
        var instant = new DateTimeOffset(2026, 10, 13, 14, 15, 16, TimeSpan.Zero);
        var values = PageClockValues.Create(instant);
        var local = instant.ToLocalTime();
        Assert.Equal(local.Hour, values["Clock.Hour"]);
        Assert.Equal(local.Minute, values["Clock.Minute"]);
        Assert.Equal(local.Second, values["Clock.Second"]);
        var rendered = new LIS2.Display.TemplateRenderer().Render("{Clock.Hour|number:0}:{Clock.Minute|number:0}", values);
        Assert.Contains(":", rendered);
    }

    [Fact]
    public void ClockValuesChangeWithTimestamp()
    {
        var instant = DateTimeOffset.UnixEpoch;
        var first = PageClockValues.Create(instant);
        var second = PageClockValues.Create(instant.AddSeconds(1));
        Assert.NotEqual(first["Clock.Second"], second["Clock.Second"]);
    }
}
