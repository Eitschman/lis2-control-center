using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageVariableBrowserCatalogTests
{
    [Theory]
    [InlineData("de", "Variablen durchsuchen…")]
    [InlineData("fr", "Parcourir les variables…")]
    [InlineData("tr", "Değişkenlere göz at…")]
    [InlineData("ru", "Просмотр переменных…")]
    [InlineData("en", "Browse variables…")]
    public void BrowserLocalizationRespectsLanguageAndEnglishFallback(string language, string expected)
    {
        Assert.Equal(expected, PageVariableBrowserLocalization.Translate("Browse variables…", language));
        Assert.Equal("Unknown key", PageVariableBrowserLocalization.Translate("Unknown key", language));
    }

    [Theory]
    [InlineData("de", "Alle")]
    [InlineData("fr", "Toutes")]
    [InlineData("tr", "Tümü")]
    [InlineData("ru", "Все")]
    public void LocalizedFiltersDoNotChangeCanonicalCategoryKeys(string language, string label)
    {
        Assert.Equal(label, PageVariableBrowserLocalization.Translate("All", language));
        Assert.Equal("Clock.Time", Assert.Single(PageVariableBrowserCatalog.Search(["Clock.Time"], null, "All", null)));
    }

    [Fact]
    public void RealHomeAssistantKeysExposeEntityAndAttributeLabels()
    {
        const string state = "HA.sensor.wohnzimmer_temperatur";
        const string attribute = "HA.sensor.wohnzimmer_temperatur.unit_of_measurement";
        var names = new Dictionary<string, string> { ["sensor.wohnzimmer_temperatur"] = "Wohnzimmer Temperatur" };
        Assert.Equal("Home Assistant", PageVariableBrowserCatalog.Category(state));
        Assert.Equal("sensor.wohnzimmer_temperatur", PageVariableBrowserCatalog.Entity(attribute));
        Assert.Equal("Wohnzimmer Temperatur · State", PageVariableBrowserCatalog.Label(state, names));
        Assert.Equal("Wohnzimmer Temperatur · unit of measurement", PageVariableBrowserCatalog.Label(attribute, names));
        Assert.Equal(2, PageVariableBrowserCatalog.Search([state, attribute], null, "Home Assistant", null).Count);
    }


    private static readonly string[] Keys =
    [
        "Clock.Time", "Clock.Date", "Hardware.CPU.Temperature.Package",
        "Hardware.CPU.Load.Total", "HomeAssistant.sensor.living_room_temperature",
        "HomeAssistant.sensor.kitchen_temperature", "Winamp.Title"
    ];

    [Fact]
    public void MultiTermSearchIsOrderIndependentAndCaseInsensitive()
    {
        var a = PageVariableBrowserCatalog.Search(Keys, "cpu temperature", "Hardware", null);
        var b = PageVariableBrowserCatalog.Search(Keys, "TEMPERATURE CPU", "Hardware", null);
        Assert.Equal(a, b);
        Assert.Equal("Hardware.CPU.Temperature.Package", Assert.Single(a));
    }

    [Fact]
    public void CategoryAndFavoritesFilterWithoutChangingOriginalKeys()
    {
        var favorites = new[] { "HomeAssistant.sensor.kitchen_temperature" };
        Assert.Equal(favorites, PageVariableBrowserCatalog.Search(Keys, null, "Favorites", favorites));
        Assert.Equal(2, PageVariableBrowserCatalog.Search(Keys, null, "Clock", favorites).Count);
        Assert.Equal("Home Assistant", PageVariableBrowserCatalog.Category(favorites[0]));
    }

    [Fact]
    public void LargeCatalogIsBoundedAndFavoritesRankFirst()
    {
        var keys = Enumerable.Range(0, 3000).Select(i => "Hardware.CPU.Load." + i).ToArray();
        var favorite = keys[2999];
        var result = PageVariableBrowserCatalog.Search(keys, "load", "Hardware", [favorite]);
        Assert.Equal(50, result.Count);
        Assert.Equal(favorite, result[0]);
    }

    [Fact]
    public void DuplicateKeysAreDeduplicatedIgnoringCase()
    {
        var result = PageVariableBrowserCatalog.Search(["Clock.Time", "clock.time"], null, "Clock", null);
        Assert.Single(result);
    }
}
