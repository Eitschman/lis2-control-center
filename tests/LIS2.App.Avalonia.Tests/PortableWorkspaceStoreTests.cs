using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class PortableWorkspaceStoreTests
{
    [Fact]
    public async Task RoundTripsPagesAndUrlWithoutAnyToken()
    {
        var path = Path.Combine(Path.GetTempPath(), "lis2-portable-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var store = new PortableWorkspaceStore(path);
            await store.SaveAsync(new PortableWorkspace
            {
                HomeAssistantUrl = "http://ha.local:8123",
                HomeAssistantEnabled = false, TransportMode = "Serial", PortName = "TEST-PORT", BrightnessPercent = 50,
                Pages = new List<PageDefinition> { new() { Name = "Clock", Line1Template = "{Clock.Time}" } }
            });
            var loaded = store.Load();
            Assert.Equal("http://ha.local:8123", loaded.HomeAssistantUrl);
            Assert.False(loaded.HomeAssistantEnabled);
            Assert.Equal("Serial", loaded.TransportMode);
            Assert.Equal("TEST-PORT", loaded.PortName);
            Assert.Equal(50, loaded.BrightnessPercent);
            Assert.Single(loaded.Pages);
            Assert.Equal("{Clock.Time}", loaded.Pages[0].Line1Template);
            Assert.DoesNotContain("AccessToken", await File.ReadAllTextAsync(path));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void MissingDeviceFieldsKeepCompatibleDefaults()
    {
        var workspace = System.Text.Json.JsonSerializer.Deserialize<PortableWorkspace>("{\"Pages\":[]}")!;
        Assert.Equal("Virtual", workspace.TransportMode);
        Assert.Null(workspace.PortName);
        Assert.Null(workspace.HomeAssistantEnabled);
        Assert.Equal(100, workspace.BrightnessPercent);
    }

    [Fact]
    public async Task HardwarePreferencesRoundTripAndLegacyDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), "lis2-hardware-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var store = new PortableWorkspaceStore(path);
            await store.SaveAsync(new PortableWorkspace { HardwareSensorPreferences = new()
            {
                new() { Key = "Hardware.cpu.Temperature.temp1", Alias = "CPU", IsFavorite = true }
            } });
            var saved = Assert.Single(store.Load().HardwareSensorPreferences);
            Assert.Equal("CPU", saved.Alias);
            Assert.True(saved.IsFavorite);
            await File.WriteAllTextAsync(path, "{\"Pages\":[]}");
            Assert.Empty(store.Load().HardwareSensorPreferences);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task CorruptJsonIsQuarantinedInsteadOfOverwritten()
    {
        var path = Path.Combine(Path.GetTempPath(), "lis2-portable-" + Guid.NewGuid().ToString("N") + ".json");
        await File.WriteAllTextAsync(path, "{broken");
        try
        {
            var store = new PortableWorkspaceStore(path);
            Assert.Empty(store.Load().Pages);
            Assert.False(File.Exists(path));
            var folder = Path.GetDirectoryName(path)!;
            Assert.NotEmpty(Directory.GetFiles(folder, Path.GetFileName(path) + ".corrupt-*"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            foreach (var backup in Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".corrupt-*"))
                File.Delete(backup);
        }
    }
}
