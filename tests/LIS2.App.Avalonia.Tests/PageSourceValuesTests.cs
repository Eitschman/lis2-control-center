using LIS2.App.Avalonia;
using LIS2.Display;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageSourceValuesTests
{
    [Fact]
    public async Task OriginalClockKeysAreAvailableAfterStart()
    {
        await using var sources = new PageSourceValues();
        await sources.StartAsync();
        var values = sources.Snapshot(DateTimeOffset.Now);
        Assert.True(values.ContainsKey("Clock.Time"));
        Assert.True(values.ContainsKey("Clock.Date"));
        Assert.True(values.ContainsKey("Clock.Day"));
        Assert.True(values.ContainsKey("Clock.Hour"));
        Assert.True(values.ContainsKey("Glyph.1"));
        var rendered = new TemplateRenderer().Render("{Clock.Time}", values);
        Assert.False(string.IsNullOrWhiteSpace(rendered));
    }

    [Fact]
    public async Task SourceLifecycleIsIdempotent()
    {
        await using var sources = new PageSourceValues();
        await sources.StartAsync();
        await sources.StartAsync();
        Assert.NotEmpty(sources.Snapshot(DateTimeOffset.Now));
    }
}
