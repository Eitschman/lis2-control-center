using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class PagePlaybackTests
{
    [Fact]
    public void RotationPauseKeepsRefreshingAndNavigationSkipsHiddenAndDisabledPages()
    {
        var playback = new PagePlayback();
        playback.Reload([
            new PageDefinition { Id = "a", Line1Template = "{Value}", DurationSeconds = 2, RefreshIntervalMilliseconds = 100 },
            new PageDefinition { Id = "hidden", VisibilityExpression = "Show=true" },
            new PageDefinition { Id = "disabled", Enabled = false },
            new PageDefinition { Id = "b", Line1Template = "Second", DurationSeconds = 2 }
        ]);
        var now = DateTimeOffset.UnixEpoch;
        var values = new Dictionary<string, object?> { ["Value"] = "Before" };
        playback.Tick(now, values);
        playback.SetRotationPaused(true, now.AddSeconds(1));
        values["Value"] = "After";
        Assert.StartsWith("After", playback.Tick(now.AddSeconds(20), values)!.Line1);
        Assert.Equal("a", playback.CurrentPageId);
        Assert.InRange(playback.NextDelay.TotalMilliseconds, 1, 1000);
        playback.RequestPage(null, 1);
        Assert.StartsWith("Second", playback.Tick(now.AddSeconds(21), values)!.Line1);
        playback.RequestPage("hidden");
        playback.Tick(now.AddSeconds(22), values);
        Assert.Equal("b", playback.CurrentPageId);
        playback.RequestPage(null, -1);
        playback.Tick(now.AddSeconds(23), values);
        Assert.Equal("a", playback.CurrentPageId);
        playback.SetRotationPaused(false, now.AddSeconds(24));
        playback.Tick(now.AddSeconds(25), values);
        Assert.Equal("a", playback.CurrentPageId);
        playback.Tick(now.AddSeconds(26), values);
        Assert.Equal("b", playback.CurrentPageId);
    }

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

    [Theory]
    [InlineData("PingPong", "BCDEFGHIJKLMNOPQRSTU")]
    [InlineData("Marquee", "BCDEFGHIJKLMNOPQRSTU")]
    [InlineData("Truncate", "ABCDEFGHIJKLMNOPQRST")]
    public void OverflowModesApplyToPagePlayback(string mode, string expected)
    {
        var playback = new PagePlayback();
        playback.Reload(new[] { new PageDefinition
        {
            Id = "scroll", Line1Template = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            Line1OverflowMode = mode, ScrollStepMilliseconds = 300,
            ScrollEdgePauseMilliseconds = 900, RefreshIntervalMilliseconds = 60000
        } });
        var now = DateTimeOffset.UnixEpoch;
        var values = new Dictionary<string, object?>();
        Assert.Equal("ABCDEFGHIJKLMNOPQRST", playback.Tick(now, values)!.Line1);
        Assert.Equal(expected, playback.Tick(now.AddMilliseconds(900), values)!.Line1);
    }

    [Fact]
    public void LinesScrollIndependentlyAndRespectStepTiming()
    {
        var playback = new PagePlayback();
        playback.Reload(new[] { new PageDefinition
        {
            Id = "scroll", Line1Template = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            Line2Template = "123456789012345678901234",
            Line1OverflowMode = "PingPong", Line2OverflowMode = "Truncate",
            ScrollStepMilliseconds = 200, ScrollEdgePauseMilliseconds = 400,
            RefreshIntervalMilliseconds = 60000
        } });
        var now = DateTimeOffset.UnixEpoch;
        var values = new Dictionary<string, object?>();
        playback.Tick(now, values);
        var frame = playback.Tick(now.AddMilliseconds(400), values)!;
        Assert.Equal("BCDEFGHIJKLMNOPQRSTU", frame.Line1);
        Assert.Equal("12345678901234567890", frame.Line2);
    }

    [Fact]
    public void NextDelayTracksRefreshAndScrollInsteadOfFixedPolling()
    {
        var playback = new PagePlayback();
        playback.Reload(new[] { new PageDefinition { Line1Template = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            RefreshIntervalMilliseconds = 5000, ScrollEdgePauseMilliseconds = 400,
            ScrollStepMilliseconds = 100, DurationSeconds = 10 } });
        var now = DateTimeOffset.UnixEpoch;
        playback.Tick(now, new Dictionary<string, object?>());
        Assert.Equal(TimeSpan.FromMilliseconds(400), playback.NextDelay);
        playback.Tick(now.AddMilliseconds(400), new Dictionary<string, object?>());
        Assert.Equal(TimeSpan.FromMilliseconds(100), playback.NextDelay);
    }

    [Fact]
    public void ScrollPositionResetsWhenPageChanges()
    {
        var playback = new PagePlayback();
        playback.Reload(new[]
        {
            new PageDefinition { Id = "a", Line1Template = "ABCDEFGHIJKLMNOPQRSTUVWXYZ", DurationSeconds = 2, ScrollEdgePauseMilliseconds = 100 },
            new PageDefinition { Id = "b", Line1Template = "123456789012345678901234", DurationSeconds = 2, ScrollEdgePauseMilliseconds = 100 }
        });
        var now = DateTimeOffset.UnixEpoch;
        var values = new Dictionary<string, object?>();
        playback.Tick(now, values);
        Assert.StartsWith("BCDE", playback.Tick(now.AddMilliseconds(100), values)!.Line1);
        Assert.Equal("12345678901234567890", playback.Tick(now.AddSeconds(2), values)!.Line1);
        Assert.Equal("ABCDEFGHIJKLMNOPQRST", playback.Tick(now.AddSeconds(4), values)!.Line1);
    }

}
