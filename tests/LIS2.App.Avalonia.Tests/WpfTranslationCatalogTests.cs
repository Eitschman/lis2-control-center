using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public class WpfTranslationCatalogTests
{
    [Theory]
    [InlineData("de", "Dashboard", "Übersicht")]
    [InlineData("de", "System Status", "Systemstatus")]
    [InlineData("de", "VFD Preview", "VFD-Vorschau")]
    [InlineData("de", "Fan Control", "Lüftersteuerung")]
    public void GermanWpfLabelsArePreserved(string language, string source, string expected)
        => Assert.Equal(expected, WpfTranslationCatalog.Translate(source, language));

    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("tr")]
    [InlineData("ru")]
    public void DynamicSensorValuesAreNeverTranslated(string language)
        => Assert.Equal("Hardware.CPU.Load.Core 0", WpfTranslationCatalog.Translate("Hardware.CPU.Load.Core 0", language));

    [Theory]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("tr")]
    [InlineData("ru")]
    public void LanguageSwitchingRestoresEnglishSource(string language)
    {
        const string original = "Dashboard";
        Assert.NotEqual(original, WpfTranslationCatalog.Translate(original, language));
        Assert.Equal(original, WpfTranslationCatalog.Translate(original, "en"));
    }

    [Fact]
    public void WpfPrefixPreservesDecorativeMarkers()
        => Assert.Equal("• Übersicht", WpfTranslationCatalog.Translate("• Dashboard", "de"));
}
