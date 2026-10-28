using LIS2.Core;
using LIS2.Display;

namespace LIS2.Display.Tests;

public sealed class TemplateRendererTests
{
    [Theory]
    [InlineData("21.5", "{HA.Room}", "21,5")]
    [InlineData("-0.0500", "{HA.Room|prefix:Wert: |suffix: °C}", "Wert: -0,0500 °C")]
    [InlineData("1.2300e+5", "{HA.Room|upper}", "1,2300E+5")]
    [InlineData("1.2300E+5", "{HA.Room|lower}", "1,2300e+5")]
    [InlineData("00123.4500", "{HA.Room}", "00123,4500")]
    [InlineData("0", "{HA.Room}", "0")]
    [InlineData("21.123456789012345678901234567890123456789", "{HA.Room}", "21,123456789012345678901234567890123456789")]
    [InlineData("21.50", "{HA.Room|percent:1}", "21,5%")]
    [InlineData("1536", "{HA.Room|bytes:1}", "1,5 KB")]
    [InlineData("Room 21.5", "{HA.Room}", "Room 21.5")]
    [InlineData("1.2.3", "{HA.Room}", "1.2.3")]
    [InlineData(" ", "{HA.Room}", " ")]
    [InlineData("unavailable", "{HA.Room|fallback:--}", "--")]
    public void Render_PreservesHaPrecisionTextAndExplicitFormatting(string state, string template, string expected)
    {
        var original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal(expected, new TemplateRenderer().Render(template,
                new Dictionary<string, object?> { ["HA.Room"] = state, ["HA.Room.Unit"] = "" }));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = original; }
    }

    [Theory]
    [InlineData("de-DE", "21,50 °C", "21,5 °C")]
    [InlineData("en-US", "21.50 °C", "21.5 °C")]
    [InlineData("fr-FR", "21,50 °C", "21,5 °C")]
    public void Render_LocalizesNumericHomeAssistantStatesWithoutChangingSourceData(string culture, string expected, string formatted)
    {
        var original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(culture);
            var values = new Dictionary<string, object?>
            {
                ["HA.sensor.room"] = "21.50", ["HA.sensor.room.Unit"] = "°C",
                ["HA.Room"] = "21.50", ["HA.Room.Unit"] = "°C",
                ["HA.sensor.room.attribute.version"] = "21.50", ["HA.sensor.room.FriendlyName"] = "Room 21.50",
                ["HA.sensor.room.attribute.target"] = 21.5
            };
            var renderer = new TemplateRenderer();
            Assert.Equal(expected, renderer.Render("{HA.sensor.room|suffix: °C}", values));
            Assert.Equal(expected, renderer.Render("{Missing|fallbackvar:HA.Room|suffix: °C}", values));
            Assert.Equal(formatted, renderer.Render("{HA.sensor.room|number:1|suffix: °C}", values));
            Assert.Equal("21.50", renderer.Render("{HA.sensor.room.attribute.version}", values));
            Assert.Equal("Room 21.50", renderer.Render("{HA.sensor.room.FriendlyName}", values));
            Assert.Equal("21.50", renderer.Render("{Missing|fallback:21.50}", values));
            Assert.Equal("21.50", values["HA.sensor.room"]);
            Assert.Equal(21.5.ToString(System.Globalization.CultureInfo.CurrentCulture), renderer.Render("{HA.sensor.room.attribute.target}", values));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = original; }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("UNAVAILABLE")]
    public void Render_UsesFallbackVariableBeforeLiteralText(string? artist)
    {
        var values = new Dictionary<string, object?> { ["Winamp.Artist"] = artist, ["Winamp.StreamName"] = "Radio Bob" };
        var renderer = new TemplateRenderer();
        const string template = "{Winamp.Artist|fallbackvar:Winamp.StreamName|fallback:--|upper|prefix:[|suffix:]}";
        Assert.Equal("[RADIO BOB]", renderer.Render(template, values));
        values["Winamp.Artist"] = "Björk";
        Assert.Equal("[BJÖRK]", renderer.Render(template, values));
        values["Winamp.Artist"] = artist;
        values["Winamp.StreamName"] = "";
        Assert.Equal("--", renderer.Render(template, values));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(false, "False")]
    [InlineData(" ", " ")]
    [InlineData("{Other}", "{Other}")]
    public void Render_FallbackVariablesPreserveValuesWithoutRecursiveEvaluation(object replacement, string expected)
    {
        var values = new Dictionary<string, object?> { ["Other"] = replacement };
        Assert.Equal(expected, new TemplateRenderer().Render("{Missing|fallbackvar:Absent|fallbackvar:Other|fallback:--}", values));
    }

    [Fact]
    public void Render_FallbackVariableReferencesCannotRecurseAndCanBeFormatted()
    {
        var renderer = new TemplateRenderer();
        Assert.Equal("--", renderer.Render("{Missing|fallbackvar:Missing|fallback:--}", new Dictionary<string, object?>()));
        Assert.Equal("5%", renderer.Render("{Missing|fallbackvar: Load |percent:0}", new Dictionary<string, object?> { ["Load"] = 5 }));
    }

    [Theory]
    [InlineData("{Load|percent:0|prefix:Core 10: }", "Core 10: 5%")]
    [InlineData("{Load|percent:0|prefix: Core 10: |suffix: load }", " Core 10: 5% load ")]
    [InlineData("{Load|percent:0|prefix: |suffix: }", " 5% ")]
    [InlineData("{ Load | percent : 0 | prefix :Core 10: }", "Core 10: 5%")]
    [InlineData("{Missing|fallback: -- }", " -- ")]
    public void Render_PreservesWhitespaceInTextArguments(string template, string expected)
    {
        var renderer = new TemplateRenderer();
        Assert.Equal(expected, renderer.Render(template, new Dictionary<string, object?> { ["Load"] = 5 }));
    }

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
