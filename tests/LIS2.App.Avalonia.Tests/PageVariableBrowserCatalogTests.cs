using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageVariableBrowserCatalogTests
{
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
