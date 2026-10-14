using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class HardwareDisplayPresetBuilderTests
{
    private static readonly IReadOnlyDictionary<string, object?> Sensors = new Dictionary<string, object?>
    {
        ["Hardware.CPU.Load.Total"] = 45d,
        ["Hardware.CPU.Temperature.Package"] = 61d,
        ["Hardware.CPU.Temperature.Package.Unit"] = "°C",
        ["Hardware.GPU.Load.Core"] = 72d,
        ["Hardware.GPU.Temperature.Core"] = 65d,
        ["Hardware.Memory.Load.Used"] = 42d,
        ["Hardware.Memory.Data.Used"] = 16d,
        ["Hardware.CPU.Fan.Fan1"] = 1200d,
        ["Hardware.CPU.Fan.Fan2"] = 1400d
    };

    [Theory]
    [InlineData("CPU", "CPU {Hardware.CPU.Load.Total}", "Temp {Hardware.CPU.Temperature.Package}°C")]
    [InlineData("GPU", "GPU {Hardware.GPU.Load.Core}", "Temp {Hardware.GPU.Temperature.Core}")]
    [InlineData("Memory", "RAM {Hardware.Memory.Load.Used}", "Used {Hardware.Memory.Data.Used}")]
    [InlineData("Temperatures", "T1 {Hardware.CPU.Temperature.Package}°C", "T2 {Hardware.GPU.Temperature.Core}")]
    [InlineData("Fans", "Fan1 {Hardware.CPU.Fan.Fan1}", "Fan2 {Hardware.CPU.Fan.Fan2}")]
    public void MatchesWpfSinglePagePresetTemplates(string preset, string line1, string line2)
    {
        var page = HardwareDisplayPresetBuilder.Build(preset, Sensors,
            Array.Empty<PortableHardwareSensorPreference>());
        Assert.NotNull(page);
        Assert.Equal(preset, page.Name);
        Assert.Equal(line1, page.Line1Template);
        Assert.Equal(line2, page.Line2Template);
    }

    [Fact]
    public void MissingSensorsDoNotCreateBlankPages()
    {
        Assert.Null(HardwareDisplayPresetBuilder.Build("Fans",
            new Dictionary<string, object?>(), Array.Empty<PortableHardwareSensorPreference>()));
    }

    [Fact]
    public void FavoritesWinWhenChoosingTwoSensors()
    {
        var preferences = new[] { new PortableHardwareSensorPreference
        {
            Key = "Hardware.GPU.Temperature.Core", IsFavorite = true
        } };
        var page = HardwareDisplayPresetBuilder.Build("Temperatures", Sensors, preferences);
        Assert.NotNull(page);
        Assert.Contains("Hardware.GPU.Temperature.Core", page.Line1Template);
    }
}
