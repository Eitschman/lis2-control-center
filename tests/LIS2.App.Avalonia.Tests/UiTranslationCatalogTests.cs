using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public class UiTranslationCatalogTests
{
    [Theory]
    [InlineData("de", "Dashboard", "Übersicht")]
    [InlineData("de", "System Status", "Systemstatus")]
    [InlineData("de", "VFD Preview", "VFD-Vorschau")]
    [InlineData("de", "Fan Control", "Lüftersteuerung")]
    public void GermanWpfLabelsArePreserved(string language, string source, string expected)
        => Assert.Equal(expected, UiTranslationCatalog.Translate(source, language));

    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("tr")]
    [InlineData("ru")]
    public void DynamicSensorValuesAreNeverTranslated(string language)
        => Assert.Equal("Hardware.CPU.Load.Core 0", UiTranslationCatalog.Translate("Hardware.CPU.Load.Core 0", language));

    [Theory]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("tr")]
    [InlineData("ru")]
    public void LanguageSwitchingRestoresEnglishSource(string language)
    {
        const string original = "Dashboard";
        Assert.NotEqual(original, UiTranslationCatalog.Translate(original, language));
        Assert.Equal(original, UiTranslationCatalog.Translate(original, "en"));
    }

    [Theory]
    [InlineData("de", "Übersicht", "Übersicht")]
    [InlineData("fr", "Tableau de bord", "Tableau de bord")]
    [InlineData("tr", "Genel Bakış", "Gösterge Paneli")]
    [InlineData("ru", "Обзор", "Панель")]
    public void TranslatedLabelsCanReturnToEnglish(string language, string translated, string catalogTranslation)
    {
        Assert.Equal("Dashboard", UiTranslationCatalog.FindEnglishSource(translated));
        Assert.Equal("Dashboard", UiTranslationCatalog.Translate(translated, "en"));
        Assert.Equal(catalogTranslation, UiTranslationCatalog.Translate("Dashboard", language));
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

    [Theory]
    [InlineData("Dashboard")]
    [InlineData("Fan Control")]
    [InlineData("System Status")]
    [InlineData("VFD Preview")]
    public void OriginalEnglishCatalogKeysAreStable(string key)
        => Assert.Equal(key, UiTranslationCatalog.FindEnglishSource(key));

    [Theory]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("tr")]
    [InlineData("ru")]
    public void WpfTranslationsRoundTripAcrossLanguages(string language)
    {
        const string key = "Dashboard";
        var localized = UiTranslationCatalog.Translate(key, language);
        Assert.Equal(key, UiTranslationCatalog.FindEnglishSource(localized));
        Assert.Equal(key, UiTranslationCatalog.Translate(localized, "en"));
    }

    [Fact]
    public void WpfPrefixPreservesDecorativeMarkers()
        => Assert.Equal("• Übersicht", UiTranslationCatalog.Translate("• Dashboard", "de"));
}
