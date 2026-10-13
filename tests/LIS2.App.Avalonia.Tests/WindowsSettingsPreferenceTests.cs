using LIS2.App.Avalonia;
using System.Text.Json;

namespace LIS2.App.Avalonia.Tests;

public sealed class WindowsSettingsPreferenceTests
{
    [Fact]
    public void ImportsOriginalSettingsWithoutInventingUnsupportedBehavior()
    {
        var imported = WindowsSettingsImporter.Import("""
        {
          "BrightnessPercent": 75,
          "ProgramCustomGlyphsOnConnect": true,
          "ThemeMode": "Dark",
          "LanguageMode": "de",
          "TransportMode": "Serial",
          "PortName": "COM3"
        }
        """);
        Assert.Equal(75, imported.BrightnessPercent);
        Assert.True(imported.ProgramCustomGlyphsOnConnect);
        Assert.Equal("Dark", imported.ThemeMode);
        Assert.Equal("de", imported.LanguageMode);
        Assert.DoesNotContain("COM3", JsonSerializer.Serialize(imported));
    }

    [Fact]
    public void InvalidBrightnessKeepsOriginalDefault()
    {
        var imported = WindowsSettingsImporter.Import("""{"BrightnessPercent":33}""");
        Assert.Equal(100, imported.BrightnessPercent);
    }

    [Fact]
    public async Task PortableSettingsRetainImportedPreferences()
    {
        var path = Path.Combine(Path.GetTempPath(), "lis2-preferences-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var store = new PortableWorkspaceStore(path);
            var imported = WindowsSettingsImporter.Import("""
            {"BrightnessPercent":50,"ProgramCustomGlyphsOnConnect":true,"ThemeMode":"Light","LanguageMode":"fr"}
            """);
            await store.SaveAsync(imported);
            var restored = store.Load();
            Assert.Equal(50, restored.BrightnessPercent);
            Assert.True(restored.ProgramCustomGlyphsOnConnect);
            Assert.Equal("Light", restored.ThemeMode);
            Assert.Equal("fr", restored.LanguageMode);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
