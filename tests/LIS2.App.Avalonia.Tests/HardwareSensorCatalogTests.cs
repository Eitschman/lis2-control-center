using LIS2.App.Avalonia;
namespace LIS2.App.Avalonia.Tests;
public sealed class HardwareSensorCatalogTests
{
    [Theory]
    [InlineData("Hardware.CPU.Temperature.Package", 45d, "°C", "45 °C")]
    [InlineData("Hardware.CPU.Load.Total", 25d, "%", "25 %")]
    [InlineData("Hardware.CPU.Fan.Fan1", 1200d, "RPM", "1200 RPM")]
    public void DisplayValueUsesSensorSpecificUnit(string key, double value, string unit, string expected)
    {
        var snapshot = new Dictionary<string, object?>
        {
            [key] = value,
            [key + ".Unit"] = unit
        };
        var entry = Assert.Single(HardwareSensorCatalog.Build(snapshot,
            Array.Empty<PortableHardwareSensorPreference>()));
        Assert.Equal(expected, entry.DisplayValue);
        Assert.Contains(expected, entry.ToString());
    }

    [Fact]
    public void TypeFilterSupportsWpfAllAndCaseInsensitiveSpecialValues()
    {
        var snapshot = new Dictionary<string, object?>
        {
            ["Hardware.CPU.Temperature.Package"] = 42d,
            ["Hardware.CPU.Mystery.Value"] = 10d
        };
        var preferences = new[] { new PortableHardwareSensorPreference
        {
            Key = "Hardware.CPU.Temperature.Package", IsFavorite = true
        } };
        Assert.Equal(2, HardwareSensorCatalog.Build(snapshot, preferences, typeFilter: "All").Count);
        Assert.Single(HardwareSensorCatalog.Build(snapshot, preferences, typeFilter: "other"));
        Assert.Single(HardwareSensorCatalog.Build(snapshot, preferences, typeFilter: "favorites"));
        Assert.Empty(HardwareSensorCatalog.Build(snapshot, preferences, typeFilter: "unavailable"));
    }

    [Fact]
    public void HardwareAndSensorNamesAreHumanReadable()
    {
        var entry = new HardwareSensorEntry("Hardware.GraphicsCard.Temperature.GPUHotSpot", 80d, "", false, true);
        Assert.Equal("Graphics Card", entry.HardwareName);
        Assert.Equal("GPU Hot Spot", entry.SensorName);
        Assert.Equal("GPU Hot Spot", entry.DisplayName);
        var underscored = new HardwareSensorEntry("Hardware.CPU.Load.Core_Usage", 15d, "", false, true);
        Assert.Equal("Core Usage", underscored.SensorName);
    }

    [Fact]
    public void SensorWithoutUnitDoesNotInventCelsius()
    {
        var entry = new HardwareSensorEntry("Hardware.CPU.Power.Package", 30d, "", false, true);
        Assert.Equal("30", entry.DisplayValue);
        Assert.DoesNotContain("°C", entry.ToString());
    }

    [Fact]
    public void TrackedDisconnectedSensorKeysRemainUnavailable()
    {
        var tracked = HardwareSensorCatalog.ExtractTrackedKeys(
            new[] { "CPU {Hardware.CPU.Temperature.Core}°C", "Fan {Hardware.CPU.Fan.1}" },
            new[] { "hardware.cpu.temperature.core", "Hardware.GPU.Fan.2" });
        Assert.Equal(3, tracked.Count);
        var rows = HardwareSensorCatalog.Build(new Dictionary<string, object?>(),
            Array.Empty<PortableHardwareSensorPreference>(), trackedKeys: tracked);
        Assert.Equal(3, rows.Count);
        Assert.All(rows, entry => Assert.False(entry.IsAvailable));
    }

    [Fact]
    public void NonNumericSensorIsUnavailable()
    {
        var rows = HardwareSensorCatalog.Build(
            new Dictionary<string, object?> { ["Hardware.CPU.Temperature.Core"] = "bad" },
            Array.Empty<PortableHardwareSensorPreference>());
        Assert.Empty(rows);
    }

    [Fact]
    public void NonNumericTrackedSensorRemainsUnavailable()
    {
        const string key = "Hardware.CPU.Temperature.Core";
        var rows = HardwareSensorCatalog.Build(new Dictionary<string, object?> { [key] = "bad" },
            Array.Empty<PortableHardwareSensorPreference>(), trackedKeys: new[] { key });
        Assert.False(Assert.Single(rows).IsAvailable);
    }

    [Fact]
    public void SearchMatchesHardwareSensorNameAndType()
    {
        var snapshot = new Dictionary<string, object?> { ["Hardware.GraphicsCard.Temperature.HotSpot"] = 80d };
        var prefs = Array.Empty<PortableHardwareSensorPreference>();
        Assert.Single(HardwareSensorCatalog.Build(snapshot, prefs, search: "graphics"));
        Assert.Single(HardwareSensorCatalog.Build(snapshot, prefs, search: "hotspot"));
        Assert.Single(HardwareSensorCatalog.Build(snapshot, prefs, search: "temperature"));
        Assert.Empty(HardwareSensorCatalog.Build(snapshot, prefs, search: "memory"));
    }

    [Fact]
    public void HardwareGroupsAreOrderedWithFavoritesFirst()
    {
        var rows = new[]
        {
            new HardwareSensorEntry("Hardware.GPU.Load.Core", 20d, "", false, true),
            new HardwareSensorEntry("Hardware.CPU.Temperature.Package", 40d, "", false, true),
            new HardwareSensorEntry("Hardware.GPU.Temperature.Core", 50d, "Favorite GPU", true, true)
        };
        var grouped = HardwareSensorEntry.GroupForDisplay(rows);
        Assert.Equal("CPU", Assert.IsType<HardwareSensorGroupHeader>(grouped[0]).Name);
        Assert.Equal("GPU", Assert.IsType<HardwareSensorGroupHeader>(grouped[2]).Name);
        Assert.Equal("Favorite GPU", Assert.IsType<HardwareSensorEntry>(grouped[3]).DisplayName);
    }

    [Fact]
    public void UnitMetadataIsNotASensor()
    {
        var snapshot = new Dictionary<string, object?>
        {
            ["Hardware.CPU.Temperature.Package"] = 45d,
            ["Hardware.CPU.Temperature.Package.Unit"] = "°C"
        };
        Assert.Single(HardwareSensorCatalog.Build(snapshot, Array.Empty<PortableHardwareSensorPreference>()));
    }

    [Fact]
    public void WpfTypeFilterIncludesUnavailableAndFavoriteEntries()
    {
        var snapshot = new Dictionary<string, object?>
        {
            ["Hardware.cpu.Temperature.temp1"] = 48d,
            ["Hardware.cpu.Load.total"] = 21d,
            ["Hardware.cpu.Unknown.foo"] = 1d
        };
        var prefs = new[]
        {
            new PortableHardwareSensorPreference { Key = "Hardware.gpu.Temperature.temp1", IsFavorite = true }
        };
        Assert.Equal(2, HardwareSensorCatalog.Build(snapshot, prefs, typeFilter: "Temperature").Count);
        Assert.Single(HardwareSensorCatalog.Build(snapshot, prefs, typeFilter: "Unavailable"));
        Assert.Single(HardwareSensorCatalog.Build(snapshot, prefs, typeFilter: "Favorites"));
        Assert.Single(HardwareSensorCatalog.Build(snapshot, prefs, typeFilter: "Other"));
        Assert.Single(HardwareSensorCatalog.Build(snapshot, prefs, typeFilter: "Load"));
        Assert.Empty(HardwareSensorCatalog.Build(snapshot, prefs, typeFilter: "Fan"));
    }

    [Fact]
    public void SensorRowsExposeWpfColumnData()
    {
        var entry = new HardwareSensorEntry("Hardware.cpu.Temperature.temp1", 42.5d, "CPU Package", true, true);
        Assert.Equal("CPU Package", entry.DisplayName);
        Assert.Equal("Temperature", entry.SensorType);
        Assert.Equal("42.5", entry.DisplayValue);
        Assert.Contains("★", entry.Markers);
        Assert.Equal("Unavailable", (entry with { IsAvailable = false }).DisplayValue);
        Assert.Equal("Other", HardwareSensorCatalog.GetSensorType("Hardware.cpu.Unknown.value"));
    }

    [Fact]
    public void MissingFavoritesRemainUnavailable()
    {
        var snapshot = new Dictionary<string, object?> { ["Hardware.cpu.Temperature.temp1"] = 47d };
        var prefs = new[] { new PortableHardwareSensorPreference { Key = "Hardware.gpu.Temperature.temp1", Alias = "GPU", IsFavorite = true } };
        var entries = HardwareSensorCatalog.Build(snapshot, prefs);
        Assert.Equal(2, entries.Count);
        Assert.Equal("GPU", entries[0].Alias);
        Assert.False(entries[0].IsAvailable);
        Assert.Contains("Unavailable", entries[0].ToString());
        Assert.True(entries[1].IsAvailable);
    }
    [Fact]
    public void AliasFilteringUsesCaseInsensitiveKeys()
    {
        var snapshot = new Dictionary<string, object?> { ["Hardware.CPU.Temperature.temp1"] = 51d };
        var prefs = new[] { new PortableHardwareSensorPreference { Key = "hardware.cpu.temperature.temp1", Alias = "Package", IsFavorite = true } };
        var entry = Assert.Single(HardwareSensorCatalog.Build(snapshot, prefs, "PACK", true));
        Assert.Equal(51d, entry.Value);
        Assert.True(entry.IsAvailable);
        Assert.True(entry.IsFavorite);
        Assert.Single(HardwareSensorCatalog.Build(snapshot, prefs));
    }
    [Fact]
    public void CaseInsensitiveSnapshotKeysDoNotDuplicateEntries()
    {
        var snapshot = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Hardware.CPU.Temperature.TEMP1"] = 40d,
            ["hardware.cpu.temperature.temp1"] = 43d
        };
        var entry = Assert.Single(HardwareSensorCatalog.Build(snapshot,
            new[] { new PortableHardwareSensorPreference { Key = "Hardware.Cpu.Temperature.Temp1", Alias = "CPU" } }));
        Assert.True(entry.IsAvailable);
        Assert.Equal(43d, entry.Value);
        Assert.Equal("CPU", entry.Alias);
    }

    [Fact]
    public void UnavailableFavoriteCanBeFilteredByAlias()
    {
        var prefs = new[] { new PortableHardwareSensorPreference { Key = "Hardware.gpu.temp1", Alias = "Graphics", IsFavorite = true } };
        var result = Assert.Single(HardwareSensorCatalog.Build(new Dictionary<string, object?>(), prefs, "GRAPH", true));
        Assert.False(result.IsAvailable);
        Assert.Equal("Hardware.gpu.temp1", result.Key);
    }
}
