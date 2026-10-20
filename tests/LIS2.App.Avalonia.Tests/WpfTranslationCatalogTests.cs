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

    [Theory]
    [InlineData("de", "Übersicht", "Übersicht")]
    [InlineData("fr", "Tableau de bord", "Tableau de bord")]
    [InlineData("tr", "Genel Bakış", "Gösterge Paneli")]
    [InlineData("ru", "Обзор", "Панель")]
    public void TranslatedLabelsCanReturnToEnglish(string language, string translated, string wpfTranslation)
    {
        Assert.Equal("Dashboard", WpfTranslationCatalog.FindEnglishSource(translated));
        Assert.Equal("Dashboard", WpfTranslationCatalog.Translate(translated, "en"));
        Assert.Equal(wpfTranslation, WpfTranslationCatalog.Translate("Dashboard", language));
    }

    [Theory]
    [InlineData("Dashboard", "Dashboard", true)]
    [InlineData("Übersicht", "Dashboard", true)]
    [InlineData("Tableau de bord", "Dashboard", true)]
    [InlineData("Gösterge Paneli", "Dashboard", true)]
    [InlineData("Панель", "Dashboard", true)]
    [InlineData("CPU: 47%", "Dashboard", false)]
    [InlineData("Artist - Track", "Dashboard", false)]
    [InlineData("sensor.cpu.package", "Dashboard", false)]
    public void StaticLocalizationDoesNotOverwriteLiveValues(string current, string original, bool expected)
        => Assert.Equal(expected, MainWindow.IsLocalizedStaticText(current, original));

    [Fact]
    public void GermanAvaloniaOverridesAreRecognized()
        => Assert.True(MainWindow.IsLocalizedStaticText("VFD-Vorschau", "VFD Preview", "VFD-Vorschau"));

    [Fact]
    public void WpfPrefixPreservesDecorativeMarkers()
        => Assert.Equal("• Übersicht", WpfTranslationCatalog.Translate("• Dashboard", "de"));
}
