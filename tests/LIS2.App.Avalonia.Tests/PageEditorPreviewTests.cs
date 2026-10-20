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
}
