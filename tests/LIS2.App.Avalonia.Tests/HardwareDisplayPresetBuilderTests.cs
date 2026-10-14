using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class HardwareDisplayPresetBuilderTests
{
    [Theory]
    [InlineData("CPU", "hardware.cpu.Temperature.package")]
    [InlineData("GPU", "hardware.gpu.Load.core")]
    [InlineData("Memory", "hardware.ram.Data.used")]
    [InlineData("Temperatures", "hardware.cpu.Temperature.package")]
    [InlineData("Fans", "hardware.cpu.Fan.fan1")]
    public void PresetSelectsOnlyMatchingKeys(string preset, string expected)
    {
        var sensors = new Dictionary<string, object?>
        {
            ["hardware.cpu.Temperature.package"] = 50,
            ["hardware.gpu.Load.core"] = 70,
            ["hardware.ram.Data.used"] = 16,
            ["hardware.cpu.Fan.fan1"] = 1000
        };
        Assert.Contains(expected, HardwareDisplayPresetBuilder.SelectKeys(sensors, preset));
    }

    [Fact]
    public void PagesUseExactTemplateKeysAndDoNotWriteHardware()
    {
        var pages = HardwareDisplayPresetBuilder.Build("CPU", new[] { "hardware.cpu.Temperature.package" });
        var page = Assert.Single(pages);
        Assert.Equal("CPU 1", page.Name);
        Assert.Equal("hardware.cpu.Temperature.package", page.Line1Template);
        Assert.Equal("{hardware.cpu.Temperature.package}", page.Line2Template);
    }
}
