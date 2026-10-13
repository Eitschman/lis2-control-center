using LIS2.Runtime;

namespace LIS2.App.Avalonia.Tests;

public sealed class LinuxHwmonFanSensorsTests
{
    [Fact]
    public void ReadsStableTemperatureKeysAndOmitsBadSensors()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var device = Path.Combine(root, "hwmon7");
        Directory.CreateDirectory(device);
        try
        {
            File.WriteAllText(Path.Combine(device, "name"), "coretemp");
            File.WriteAllText(Path.Combine(device, "temp1_input"), "51500");
            File.WriteAllText(Path.Combine(device, "temp1_label"), "Package id 0");
            File.WriteAllText(Path.Combine(device, "temp2_input"), "invalid");
            var values = new LinuxHwmonFanSensors(root).Snapshot();
            Assert.Equal(51.5, values["Hardware.coretemp.Temperature.temp1"]);
            Assert.Equal(51.5, values["Hardware.coretemp.Temperature.Package_id_0"]);
            Assert.False(values.ContainsKey("Hardware.coretemp.Temperature.temp2"));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
