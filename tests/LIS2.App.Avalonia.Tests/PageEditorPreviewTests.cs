using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageEditorPreviewTests
{
    [Fact]
    public void PreviewUsesSameTemplateAndTwentyColumnOutputAsPlayback()
    {
        var values = new Dictionary<string, object?> { ["Test.Value"] = "OK" };
        var frame = PageEditorPreview.Render("Value {Test.Value}", "Line two",
            "Truncate", "Truncate", 250, 800, values);
        Assert.Equal("Value OK".PadRight(20), frame.Line1);
        Assert.Equal("Line two".PadRight(20), frame.Line2);
    }

    [Theory]
    [InlineData("Truncate")]
    [InlineData("PingPong")]
    [InlineData("Marquee")]
    public void PreviewStartsAtFirstVisibleTwentyColumns(string overflow)
    {
        var frame = PageEditorPreview.Render("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "",
            overflow, overflow, 250, 800, new Dictionary<string, object?>());
        Assert.Equal("ABCDEFGHIJKLMNOPQRST", frame.Line1);
    }

    [Fact]
    public void PreviewIsSideEffectFreeForSourceValues()
    {
        var values = new Dictionary<string, object?> { ["Test.Value"] = "Before" };
        PageEditorPreview.Render("{Test.Value}", "", "PingPong", "Truncate", 250, 800, values);
        Assert.Equal("Before", values["Test.Value"]);
    }

    [Theory]
    [InlineData("PingPong")]
    [InlineData("Marquee")]
    public void PreviewAdvancesWithClockWithoutMutatingSource(string overflow)
    {
        var preview = new PageEditorPreview();
        var values = new Dictionary<string, object?> { ["Long"] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" };
        preview.Configure("{Long}", "", overflow, "Truncate", 100, 200);
        var start = DateTimeOffset.UnixEpoch;
        var first = preview.Tick(start, values);
        var later = preview.Tick(start.AddMilliseconds(350), values);
        Assert.NotEqual(first.Line1, later.Line1);
        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWXYZ", values["Long"]);
    }

    [Fact]
    public void ReconfiguringDraftResetsScrollAndKeepsEarlierDraftIndependent()
    {
        var preview = new PageEditorPreview();
        var now = DateTimeOffset.UnixEpoch;
        var values = new Dictionary<string, object?>();
        preview.Configure("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "", "Marquee", "Truncate", 100, 0);
        preview.Tick(now, values);
        preview.Tick(now.AddSeconds(1), values);
        preview.Configure("New draft", "", "Truncate", "Truncate", 100, 0);
        Assert.Equal("New draft".PadRight(20), preview.Tick(now.AddSeconds(2), values).Line1);
    }

    [Fact]
    public void UnconfiguredPreviewCannotTick()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new PageEditorPreview().Tick(DateTimeOffset.UnixEpoch, new Dictionary<string, object?>()));
    }

    [Fact]
    public void LiveValuesRefreshWithoutReconfiguringDraft()
    {
        var preview = new PageEditorPreview();
        var values = new Dictionary<string, object?> { ["Clock"] = "12:00" };
        preview.Configure("Time {Clock}", "", "Truncate", "Truncate", 100, 200, 250);
        var start = DateTimeOffset.UnixEpoch;
        Assert.Equal("Time 12:00".PadRight(20), preview.Tick(start, values).Line1);
        values["Clock"] = "12:01";
        Assert.Equal("Time 12:01".PadRight(20), preview.Tick(start.AddMilliseconds(300), values).Line1);
    }

    [Fact]
    public void BothLinesScrollIndependently()
    {
        var preview = new PageEditorPreview();
        preview.Configure("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "0123456789ABCDEFGHIJKLM",
            "Marquee", "PingPong", 100, 200);
        var values = new Dictionary<string, object?>();
        var first = preview.Tick(DateTimeOffset.UnixEpoch, values);
        var later = preview.Tick(DateTimeOffset.UnixEpoch.AddMilliseconds(350), values);
        Assert.NotEqual(first.Line1, later.Line1);
        Assert.NotEqual(first.Line2, later.Line2);
        Assert.Equal(20, later.Line1.Length);
        Assert.Equal(20, later.Line2.Length);
    }

    [Fact]
    public void ShortTextDoesNotScheduleScroll()
    {
        var preview = new PageEditorPreview();
        preview.Configure("Short", "OK", "Marquee", "PingPong", 100, 200);
        var frame = preview.Tick(DateTimeOffset.UnixEpoch, new Dictionary<string, object?>());
        Assert.Equal("Short".PadRight(20), frame.Line1);
        Assert.True(preview.NextDelay > TimeSpan.Zero);
    }

    [Fact]
    public void SourceValuesRespectDraftRefreshInterval()
    {
        var preview = new PageEditorPreview();
        var values = new Dictionary<string, object?> { ["Value"] = "Before" };
        preview.Configure("{Value}", "", "Truncate", "Truncate", 100, 200, 1000);
        var start = DateTimeOffset.UnixEpoch;
        Assert.Equal("Before".PadRight(20), preview.Tick(start, values).Line1);
        values["Value"] = "After";
        Assert.Equal("Before".PadRight(20), preview.Tick(start.AddMilliseconds(999), values).Line1);
        Assert.Equal("After".PadRight(20), preview.Tick(start.AddMilliseconds(1000), values).Line1);
    }

    [Fact]
    public void UnchangedDraftDoesNotResetScrollingPosition()
    {
        var preview = new PageEditorPreview();
        var values = new Dictionary<string, object?>();
        preview.Configure("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "", "PingPong", "Truncate", 100, 200);
        var start = DateTimeOffset.UnixEpoch;
        preview.Tick(start, values);
        var advanced = preview.Tick(start.AddMilliseconds(350), values);
        preview.Configure("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "", "PingPong", "Truncate", 100, 200);
        Assert.Equal(advanced.Line1, preview.Tick(start.AddMilliseconds(350), values).Line1);
    }

    [Fact]
    public void ChangingRefreshCadenceReconfiguresPreview()
    {
        var preview = new PageEditorPreview();
        var values = new Dictionary<string, object?> { ["Value"] = "A" };
        preview.Configure("{Value}", "", "Truncate", "Truncate", 100, 200, 1000);
        preview.Tick(DateTimeOffset.UnixEpoch, values);
        preview.Configure("{Value}", "", "Truncate", "Truncate", 100, 200, 100);
        values["Value"] = "B";
        Assert.Equal("B".PadRight(20),
            preview.Tick(DateTimeOffset.UnixEpoch.AddMilliseconds(200), values).Line1);
    }
}
