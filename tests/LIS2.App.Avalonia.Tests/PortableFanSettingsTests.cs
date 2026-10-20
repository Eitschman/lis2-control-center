using System.Text.Json;
using LIS2.App.Avalonia;
using LIS2.Fans;

namespace LIS2.App.Avalonia.Tests;

public sealed class PortableFanSettingsTests
{
    [Fact]
    public void ManualOutputHonorsLimitsAndStopWithoutChangingConfiguredModes()
    {
        var settings = new PortableFanSettings();
        settings.Channels[0].Mode = "Curve";
        settings.Channels[0].MinimumPercent = 40;
        settings.Channels[1].MaximumPercent = 70;
        settings.Channels[2].AllowStop = true;
        Assert.Equal(new[] { 40, 70, 0, 30 }, settings.CalculateManualOutputs(new[] { 0, 100, 0, 0 }));
        Assert.Equal("Curve", settings.Channels[0].Mode);
        Assert.Throws<ArgumentException>(() => settings.CalculateManualOutputs(new[] { 0, 100, 0 }));
        Assert.Throws<ArgumentException>(() => settings.CalculateManualOutputs(new[] { 0, 100, 0, 101 }));
    }

    [Fact]
    public void FourWpfCompatibleDefaults()
    {
        var settings = new PortableFanSettings();
        Assert.Equal(4, settings.Channels.Count);
        Assert.All(settings.Channels, c => { Assert.Equal("Fixed", c.Mode); Assert.Equal(30, c.MinimumPercent); Assert.Equal(100, c.FailSafePercent); Assert.Equal(3, c.Curve.Count); });
    }

    [Fact]
    public void WindowsSettingsImportPreservesSafetyConfiguration()
    {
        const string json = """
        {"Fans":{"AutomaticControlEnabled":true,"Channels":[
          {"Name":"CPU","Mode":"Curve","SensorKey":"CPU.Temp","MinimumPercent":25,
           "MaximumPercent":90,"FailSafePercent":90,"HysteresisDegrees":1.5,
           "Curve":[{"Temperature":40,"OutputPercent":30},{"Temperature":70,"OutputPercent":90}]}
        ]}}
        """;
        var imported = WindowsSettingsImporter.Import(json);
        Assert.True(imported.Fans.AutomaticControlEnabled);
        Assert.Equal(4, imported.Fans.Channels.Count);
        Assert.Equal("CPU.Temp", imported.Fans.Channels[0].SensorKey);
        Assert.Equal(FanMode.Curve, imported.Fans.ToRuntimeChannels()[0].Mode);
        Assert.Equal(90, imported.Fans.ToRuntimeChannels()[0].FailSafePercent);
        Assert.Equal("Fan 4", imported.Fans.Channels[3].Name);
        Assert.DoesNotContain("AccessToken", JsonSerializer.Serialize(imported));
    }

    [Fact]
    public void InvalidSettingsAreNormalized()
    {
        var source = new PortableFanSettings { Channels = new List<PortableFanChannel>
        {
            new() { MinimumPercent = -10, MaximumPercent = 200, FailSafePercent = 500,
                Mode = "unknown", HysteresisDegrees = double.NaN }
        }};
        var actual = PortableFanSettings.Normalize(source).Channels[0];
        Assert.Equal(0, actual.MinimumPercent);
        Assert.Equal(100, actual.MaximumPercent);
        Assert.Equal(100, actual.FailSafePercent);
        Assert.Equal("Fixed", actual.Mode);
        Assert.Equal(1, actual.HysteresisDegrees);
    }

    [Fact]
    public async Task PortableWorkspacePersistsFanSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new PortableWorkspaceStore(path);
            var workspace = new PortableWorkspace();
            workspace.Fans.Channels[2].FixedPercent = 67;
            await store.SaveAsync(workspace);
            Assert.Equal(67, store.Load().Fans.Channels[2].FixedPercent);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
