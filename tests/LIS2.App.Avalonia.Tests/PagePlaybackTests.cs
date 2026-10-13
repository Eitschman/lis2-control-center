using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class PagePlaybackTests
{
    [Fact]
    public void RotatesByDurationAndSkipsDisabledPages()
    {
        var playback = new PagePlayback();
        playback.Reload(new[]
        {
            new PageDefinition { Id = "a", Line1Template = "First", DurationSeconds = 2 },
            new PageDefinition { Id = "disabled", Line1Template = "Disabled", Enabled = false },
            new PageDefinition { Id = "b", Line1Template = "Second", DurationSeconds = 2 }
        });
        var now = DateTimeOffset.UnixEpoch;
        var values = new Dictionary<string, object?>();
        Assert.StartsWith("First", playback.Tick(now, values)!.Line1);
        Assert.StartsWith("First", playback.Tick(now.AddSeconds(1), values)!.Line1);
        Assert.StartsWith("Second", playback.Tick(now.AddSeconds(2), values)!.Line1);
        Assert.StartsWith("First", playback.Tick(now.AddSeconds(4), values)!.Line1);
    }

    [Fact]
    public void ConditionsSwitchImmediatelyAndDoNotShowInvisiblePages()
    {
        var playback = new PagePlayback();
        playback.Reload(new[]
        {
            new PageDefinition { Id = "playing", Line1Template = "Playing", VisibilityExpression = "Media.State=Playing" },
            new PageDefinition { Id = "paused", Line1Template = "Paused", VisibilityExpression = "Media.State=Paused" }
        });
        var values = new Dictionary<string, object?> { ["Media.State"] = "Playing" };
        Assert.Equal("playing", playback.Tick(DateTimeOffset.UnixEpoch, values) is null ? null : playback.CurrentPageId);
        values["Media.State"] = "Paused";
        Assert.StartsWith("Paused", playback.Tick(DateTimeOffset.UnixEpoch.AddSeconds(1), values)!.Line1);
        values["Media.State"] = "Stopped";
        Assert.Null(playback.Tick(DateTimeOffset.UnixEpoch.AddSeconds(2), values));
    }

    [Fact]
    public void EmptyOrAllDisabledPagesReturnNoFrame()
    {
        var playback = new PagePlayback();
        playback.Reload(new[] { new PageDefinition { Enabled = false } });
        Assert.Null(playback.Tick(DateTimeOffset.UnixEpoch, new Dictionary<string, object?>()));
    }
    [Fact]
    public void RefreshIntervalCachesValuesUntilNextRefresh()
    {
        var playback = new PagePlayback();
        playback.Reload(new[] { new PageDefinition
        {
            Id = "dynamic", Line1Template = "{Test.Value}",
            RefreshIntervalMilliseconds = 2000, DurationSeconds = 10
        } });
        var now = DateTimeOffset.UnixEpoch;
        var values = new Dictionary<string, object?> { ["Test.Value"] = "Before" };
        Assert.StartsWith("Before", playback.Tick(now, values)!.Line1);
        values["Test.Value"] = "After";
        Assert.StartsWith("Before", playback.Tick(now.AddMilliseconds(1000), values)!.Line1);
        Assert.StartsWith("After", playback.Tick(now.AddMilliseconds(2000), values)!.Line1);
    }

    [Fact]
    public void PageChangeRendersImmediatelyEvenWhenRefreshIsLong()
    {
        var playback = new PagePlayback();
        playback.Reload(new[]
        {
            new PageDefinition { Id = "first", Line1Template = "First", DurationSeconds = 1, RefreshIntervalMilliseconds = 60000 },
            new PageDefinition { Id = "second", Line1Template = "Second", DurationSeconds = 1, RefreshIntervalMilliseconds = 60000 }
        });
        var now = DateTimeOffset.UnixEpoch;
        var values = new Dictionary<string, object?>();
        Assert.StartsWith("First", playback.Tick(now, values)!.Line1);
        Assert.StartsWith("Second", playback.Tick(now.AddSeconds(1), values)!.Line1);
    }

}
