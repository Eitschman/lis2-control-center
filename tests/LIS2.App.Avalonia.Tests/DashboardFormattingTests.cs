using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class DashboardFormattingTests
{
    [Theory]
    [InlineData("de-DE", "41,4 °C / 8,2 %")]
    [InlineData("en-US", "41.4 °C / 8.2 %")]
    public void CompactFormattingUsesRegionalSeparatorsWithoutScalingPercent(string culture, string expected)
    {
        var original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(culture);
            var template = DashboardFormatting.CompactTemplate("{Temperature|number:2|suffix:°C} / {Load|percent:2}");
            Assert.Equal(expected, new LIS2.Display.TemplateRenderer().Render(template,
                new Dictionary<string, object?> { ["Temperature"] = 41.35, ["Load"] = 8.21 }));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = original; }
    }

    [Theory]
    [InlineData("{CPU|number:2|suffix:°C}", "{CPU|number:1|suffix: °C}")]
    [InlineData("{CPU|percent:2}", "{CPU|number:1|suffix: %}")]
    [InlineData("{RPM|number:0|suffix:RPM}", "{RPM|number:0|suffix: RPM}")]
    [InlineData("{Disk|bytes:3}", "{Disk|bytes:1}")]
    [InlineData("{Clock.Time} 50.123", "{Clock.Time} 50.123")]
    [InlineData("{CPU|fallback:50.123|number:2|prefix:Core 10: |suffix: °C}", "{CPU|fallback:50.123|number:1|prefix:Core 10: |suffix: °C}")]
    [InlineData("{CPU|number:invalid|suffix:°C}", "{CPU|number:invalid|suffix:°C}")]
    [InlineData("{CPU|number:1|suffix:  °C }", "{CPU|number:1|suffix:  °C }")]
    public void CompactsOnlyNumericOperationsAndExactUnits(string template, string expected)
    {
        Assert.Equal(expected, DashboardFormatting.CompactTemplate(template));
        Assert.Equal(expected, DashboardFormatting.CompactTemplate(expected));
    }
}
