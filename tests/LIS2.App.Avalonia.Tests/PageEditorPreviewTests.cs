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
        preview.Configure("{Long}", "", overflow, "Truncate", 100, 0);
        var start = DateTimeOffset.UnixEpoch;
        var first = preview.Tick(start, values);
        var later = preview.Tick(start.AddMilliseconds(500), values);
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
}
