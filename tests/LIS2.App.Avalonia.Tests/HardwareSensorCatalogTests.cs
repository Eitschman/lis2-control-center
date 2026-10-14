using LIS2.App.Avalonia;
namespace LIS2.App.Avalonia.Tests;
public sealed class HardwareSensorCatalogTests
{
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
        Assert.False(Assert.Single(rows).IsAvailable);
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
            ["CPU.TEMP1"] = 40d,
            ["cpu.temp1"] = 43d
        };
        var entry = Assert.Single(HardwareSensorCatalog.Build(snapshot,
            new[] { new PortableHardwareSensorPreference { Key = "Cpu.Temp1", Alias = "CPU" } }));
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
