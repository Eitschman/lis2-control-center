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
    [Fact]
    public void Render_UsesFallbackForMissingAndUnavailableValues()
    {
        var renderer = new TemplateRenderer();
        var values = new Dictionary<string, object?>
        {
            ["HA.sensor.one"] = "unavailable",
            ["HA.sensor.two"] = "unknown"
        };

        Assert.Equal("--", renderer.Render("{HA.sensor.missing|fallback:--}", values));
        Assert.Equal("--", renderer.Render("{HA.sensor.one|fallback:--}", values));
        Assert.Equal("--", renderer.Render("{HA.sensor.two|fallback:--}", values));
    }

    [Theory]
    [InlineData("en-US", "21.5°C", "1.0 GB")]
    [InlineData("de-DE", "21,5°C", "1,0 GB")]
    [InlineData("fr-FR", "21,5°C", "1,0 GB")]
    public void Render_FormatsNumberPercentBytesAndAffixes(string culture, string temperature, string bytes)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new(culture);
            var renderer = new TemplateRenderer();
            var values = new Dictionary<string, object?>
            {
                ["Temp"] = 21.456,
                ["Load"] = 73.2,
                ["Disk"] = 1073741824L
            };

            Assert.Equal(
                temperature,
                renderer.Render("{Temp|number:1|suffix:°C}", values));
            Assert.Equal(
                "73%",
                renderer.Render("{Load|percent:0}", values));
            Assert.Equal(
                bytes,
                renderer.Render("{Disk|bytes:1}", values));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void Render_AllowsFormattingPipelineWithFallback()
    {
        var renderer = new TemplateRenderer();
        var values = new Dictionary<string, object?>
        {
            ["Name"] = "lis2"
        };

        Assert.Equal(
            "[LIS2]",
            renderer.Render("{Name|upper|prefix:[|suffix:]|fallback:--}", values));
    }
}
