using System.Globalization;
using LIS2.App.Avalonia;
using LIS2.Display;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageTemplateFormatTests
{
    [Fact]
    public void GeneratedAffixesPreserveLeadingTrailingAndSpaceOnlyArguments()
    {
        var format = new PageTemplateFormat("Load", 2, 0, 0, " Core 10: ", " load ", " -- ");
        Assert.True(format.TryBuild(out var template));
        Assert.Equal("{Load|fallback: -- |percent:0|prefix: Core 10: |suffix: load }", template);
        var renderer = new TemplateRenderer();
        Assert.Equal(" Core 10: 5% load ", renderer.Render(template, new Dictionary<string, object?> { ["Load"] = 5 }));
        Assert.Equal(" -- ", renderer.Render(template, new Dictionary<string, object?>()));
        Assert.True((format with { Prefix = " ", Suffix = " " }).TryBuild(out var spaces));
        Assert.Equal(" 5% ", renderer.Render(spaces, new Dictionary<string, object?> { ["Load"] = 5 }));
    }

    [Theory]
    [InlineData("de-DE", "50,12°C", "73,2%", "1,00 GB")]
    [InlineData("en-US", "50.12°C", "73.2%", "1.00 GB")]
    public void GeneratedFormatsAndDocumentedExamplesUseActualRenderer(string culture, string temperature, string percent, string bytes)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new(culture);
            var renderer = new TemplateRenderer();
            string Render(PageTemplateFormat format, object? value)
            {
                Assert.True(format.TryBuild(out var template));
                return renderer.Render(template, new Dictionary<string, object?> { ["Value"] = value });
            }
            var number = new PageTemplateFormat("Value", 1, 2, 0, "", "°C", "--");
            Assert.Equal(temperature, Render(number, "50.1234567"));
            Assert.Equal(temperature, Render(number, 50.1234567));
            Assert.Equal("--", Render(number, null));
            Assert.Equal("--", Render(number, "unavailable"));
            Assert.Equal(percent, Render(number with { Kind = 2, Decimals = 1, Suffix = "" }, 73.24));
            Assert.Equal("1%", Render(number with { Kind = 2, Decimals = 0, Suffix = "" }, 0.75));
            Assert.Equal(bytes, Render(number with { Kind = 3, Suffix = "" }, 1073741824));
            Assert.Equal("[LIS2]", Render(number with { Kind = 0, LetterCase = 1, Prefix = "[", Suffix = "]" }, "lis2"));
            Assert.Equal("lis2", Render(number with { Kind = 0, LetterCase = 2, Suffix = "" }, "LIS2"));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData("", "", "", "")]
    [InlineData("Bad Key", "", "", "")]
    [InlineData("Value|upper", "", "", "")]
    [InlineData("{Value}", "", "", "")]
    [InlineData("Value", "|upper", "", "")]
    [InlineData("Value", "", "{Clock.Time}", "")]
    [InlineData("Value", "", "", "}|upper")]
    public void RejectsInputsThatWouldChangeTemplateStructure(string key, string prefix, string suffix, string fallback) =>
        Assert.False(new PageTemplateFormat(key, 1, 2, 0, prefix, suffix, fallback).TryBuild(out _));
}
