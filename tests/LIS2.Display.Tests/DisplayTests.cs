using LIS2.Display;

namespace LIS2.Display.Tests;

public sealed class DisplayTests
{
    [Fact]
    public void Frame_AlwaysHasTwentyCells()
    {
        var frame = DisplayFrame.Create("ABC", "XYZ");

        Assert.Equal(20, frame.Line1.Length);
        Assert.Equal(20, frame.Line2.Length);
        Assert.StartsWith("ABC", frame.Line1);
        Assert.StartsWith("XYZ", frame.Line2);
    }

    [Fact]
    public void Frame_TruncatesLongLines()
    {
        var frame = DisplayFrame.Create("1234567890123456789012345", string.Empty);
        Assert.Equal("12345678901234567890", frame.Line1);
    }

    [Fact]
    public void Frame_TransliteratesGermanCharacters()
    {
        var frame = DisplayFrame.Create("Dümmer", string.Empty);
        Assert.StartsWith("Duemmer", frame.Line1);
    }

    [Fact]
    public void TemplateRenderer_ReplacesVariables()
    {
        var renderer = new TemplateRenderer();
        var values = new Dictionary<string, object?>
        {
            ["Winamp.Artist"] = "Iron Maiden",
            ["Winamp.Title"] = "The Trooper"
        };

        var frame = renderer.RenderFrame("{Winamp.Artist}", "{Winamp.Title}", values);

        Assert.Equal("Iron Maiden         ", frame.Line1);
        Assert.Equal("The Trooper         ", frame.Line2);
    }

    [Fact]
    public void TemplateRenderer_UnknownVariableBecomesEmpty()
    {
        var renderer = new TemplateRenderer();
        Assert.Equal("CPU ", renderer.Render("CPU {CPU.Load}", new Dictionary<string, object?>()));
    }
}
