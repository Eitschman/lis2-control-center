using LIS2.Core;
using LIS2.Display;

namespace LIS2.Display.Tests;

public sealed class TemplateRendererTests
{
    [Fact]
    public void Render_ResolvesNamedGlyphValue()
    {
        var renderer = new TemplateRenderer();
        var glyph = Lis2Protocol.CustomGlyph(1).ToString();
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Glyph.Play"] = glyph
        };

        Assert.Equal($"{glyph} NOW PLAYING", renderer.Render("{Glyph.Play} NOW PLAYING", values));
    }
}
