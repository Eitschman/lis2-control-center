using LIS2.App.Avalonia;
namespace LIS2.App.Avalonia.Tests;
public sealed class HardwareSensorCatalogTests
{
    [Fact]
    public void FormattedPageTemplatesTrackOriginalOfflineSensorKeys()
    {
        const string temperature = "Hardware.CPU.Temperature.Package";
        const string load = "Hardware.CPU.Load.Total";
        var tracked = HardwareSensorCatalog.ExtractTrackedKeys(
            new[] { "CPU {" + temperature + ":F1} / {" + load + ":0}%",
                    "Again {" + temperature + "} and unrelated {HomeAssistant.sensor.temp}" },
            Array.Empty<string>());
        Assert.Equal(2, tracked.Count);
        Assert.Contains(temperature, tracked);
        Assert.Contains(load, tracked);
        var entries = HardwareSensorCatalog.Build(new Dictionary<string, object?>(),
            Array.Empty<PortableHardwareSensorPreference>(), trackedKeys: tracked);
        Assert.Equal(2, entries.Count);
        Assert.All(entries, entry => Assert.False(entry.IsAvailable));
    }


    [Theory]
    [InlineData(0, "All types")]
    [InlineData(1, "Favorites")]
    [InlineData(2, "Unavailable")]
    [InlineData(3, "Temperature")]
    [InlineData(4, "Load")]
    [InlineData(5, "Fan")]
    [InlineData(6, "Clock")]
    [InlineData(7, "Voltage")]
    [InlineData(8, "Power")]
    [InlineData(9, "Data")]
    [InlineData(10, "Throughput")]
    [InlineData(11, "Other")]
    public void HardwareFilterUsesStableKeysIndependentOfLocalizedLabels(int index, string expected)
        => Assert.Equal(expected, MainWindow.HardwareTypeFilterKey(index));

    [Fact]
    public void StableFilterKeysPreserveSensorResults()
    {
        var values = new Dictionary<string, object?>
        {
            ["Hardware.CPU.Temperature.Package"] = 55d,
            ["Hardware.CPU.Load.Total"] = 32d
        };
        var preferences = Array.Empty<PortableHardwareSensorPreference>();
        Assert.Single(HardwareSensorCatalog.Build(values, preferences,
            typeFilter: MainWindow.HardwareTypeFilterKey(3)));
        Assert.Single(HardwareSensorCatalog.Build(values, preferences,
            typeFilter: MainWindow.HardwareTypeFilterKey(4)));
        Assert.Equal(2, HardwareSensorCatalog.Build(values, preferences,
            typeFilter: MainWindow.HardwareTypeFilterKey(0)).Count);
    }

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
    public void InvalidAndNonfiniteSensorValuesDoNotBecomeAvailable()
    {
        var snapshot = new Dictionary<string, object?>
        {
            ["Hardware.CPU.Temperature.NaN"] = double.NaN,
            ["Hardware.CPU.Temperature.PositiveInfinity"] = double.PositiveInfinity,
            ["Hardware.CPU.Temperature.NegativeInfinity"] = double.NegativeInfinity,
            ["Hardware.CPU.Temperature.Invalid"] = "broken",
            ["Hardware.CPU.Temperature.Valid"] = 40d
        };
        var tracked = snapshot.Keys.ToArray();
        var entries = HardwareSensorCatalog.Build(snapshot,
            Array.Empty<PortableHardwareSensorPreference>(), trackedKeys: tracked);
        Assert.Equal(5, entries.Count);
        Assert.Single(entries, entry => entry.IsAvailable);
        Assert.All(entries.Where(entry => entry.Key != "Hardware.CPU.Temperature.Valid"),
            entry => Assert.False(entry.IsAvailable));
        Assert.Single(HardwareSensorCatalog.Build(snapshot,
            Array.Empty<PortableHardwareSensorPreference>()));
    }

    [Fact]
    public void SelectedSensorDetailsMatchWpfSelectionPanel()
    {
        var available = new HardwareSensorEntry("Hardware.CPU.Temperature.Package", 45d,
            "Processor", true, true, "°C");
        Assert.Equal("Processor — 45 °C" + Environment.NewLine +
            "Hardware.CPU.Temperature.Package", available.SelectionDetails);
        var unavailable = available with { IsAvailable = false };
        Assert.Contains("Unavailable", unavailable.SelectionDetails);
        Assert.Contains("Hardware.CPU.Temperature.Package", unavailable.SelectionDetails);
    }

    [Fact]
    public void HardwareSummaryCountsRawValuesAndConfiguredUnavailableSensors()
    {
        var snapshot = new Dictionary<string, object?>
        {
            ["Hardware.CPU.Temperature.Core"] = 45d,
            ["Hardware.CPU.Temperature.Core.Unit"] = "°C",
            ["Hardware.CPU.Load.Total"] = 30d
        };
        var tracked = new[] { "Hardware.GPU.Fan.Missing" };
        var visible = HardwareSensorCatalog.Build(snapshot,
            Array.Empty<PortableHardwareSensorPreference>(), trackedKeys: tracked);
        Assert.Equal("3 visible sensor(s) / 2 total values · 1 unavailable configured sensor(s)",
            HardwareSensorCatalog.BuildSummary(snapshot, visible));
        Assert.Equal("Hardware source error: sensor offline",
            HardwareSensorCatalog.BuildSummary(snapshot, visible, "sensor offline"));
        Assert.Equal("1 visible sensor(s) / 2 total values",
            HardwareSensorCatalog.BuildSummary(snapshot,
                HardwareSensorCatalog.Build(snapshot, Array.Empty<PortableHardwareSensorPreference>(),
                    typeFilter: "Temperature")));
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
