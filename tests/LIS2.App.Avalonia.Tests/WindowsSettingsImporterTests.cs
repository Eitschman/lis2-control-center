using LIS2.App.Avalonia;
using System.Text.Json;

namespace LIS2.App.Avalonia.Tests;

public sealed class WindowsSettingsImporterTests
{
    [Fact]
    public void ImportsOriginalPagesGlyphsAndPreferencesButNeverToken()
    {
        var source = new
        {
            Pages = new[] { new PageDefinition
            {
                Id = "clock", Name = "Clock", Line1Template = "{Clock.Time}",
                Line2Template = "{Clock.Date}", DurationSeconds = 5
            } },
            CustomGlyphs = Enumerable.Range(1, 8).Select(n => new CustomGlyphSettings
            {
                Name = $"Glyph {n}", Rows = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
            }).ToArray(),
            HomeAssistant = new
            {
                Url = "https://ha.example.test",
                AccessToken = "dpapi:v1:SHOULD_NOT_BE_COPIED",
                EntityPreferences = new[] { new { EntityId = "sensor.room", Alias = "Room", IsFavorite = true } }
            }
        };
        var imported = WindowsSettingsImporter.Import(JsonSerializer.Serialize(source));
        Assert.Single(imported.Pages);
        Assert.Equal("{Clock.Time}", imported.Pages[0].Line1Template);
        Assert.Equal(8, imported.CustomGlyphs.Count);
        Assert.Equal("Glyph 8", imported.CustomGlyphs[7].Name);
        Assert.Equal("https://ha.example.test", imported.HomeAssistantUrl);
        Assert.Single(imported.HomeAssistantEntityPreferences);
        Assert.True(imported.HomeAssistantEntityPreferences[0].IsFavorite);
        Assert.DoesNotContain("SHOULD_NOT_BE_COPIED", JsonSerializer.Serialize(imported));
        Assert.DoesNotContain("AccessToken", JsonSerializer.Serialize(imported));
    }

    [Fact]
    public void InvalidOriginalPageConfigurationIsRejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            WindowsSettingsImporter.Import("{\"Pages\":[{\"Id\":\"bad\",\"DurationSeconds\":0}]}"));
    }

    [Fact]
    public void InvalidGlyphCountIsRejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            WindowsSettingsImporter.Import("{\"CustomGlyphs\":[]}"));
    }

    [Fact]
    public async Task ImportedGlyphsSurvivePortableWorkspaceRoundTrip()
    {
        var path = Path.Combine(Path.GetTempPath(), "lis2-migration-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var imported = WindowsSettingsImporter.Import(JsonSerializer.Serialize(new
            {
                CustomGlyphs = Enumerable.Range(0, 8).Select(i => new CustomGlyphSettings
                {
                    Name = $"Saved {i}", Rows = new byte[8]
                }).ToArray()
            }));
            var store = new PortableWorkspaceStore(path);
            await store.SaveAsync(imported);
            var restored = store.Load();
            Assert.Equal(8, restored.CustomGlyphs.Count);
            Assert.Equal("Saved 7", restored.CustomGlyphs[7].Name);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
