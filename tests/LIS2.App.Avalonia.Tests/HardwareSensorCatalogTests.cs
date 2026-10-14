using LIS2.App.Avalonia;
namespace LIS2.App.Avalonia.Tests;
public sealed class HardwareSensorCatalogTests
{
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
    public void UnavailableFavoriteCanBeFilteredByAlias()
    {
        var prefs = new[] { new PortableHardwareSensorPreference { Key = "Hardware.gpu.temp1", Alias = "Graphics", IsFavorite = true } };
        var result = Assert.Single(HardwareSensorCatalog.Build(new Dictionary<string, object?>(), prefs, "GRAPH", true));
        Assert.False(result.IsAvailable);
        Assert.Equal("Hardware.gpu.temp1", result.Key);
    }
}
