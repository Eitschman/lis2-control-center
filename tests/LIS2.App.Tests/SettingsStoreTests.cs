using System.Text.Json;

namespace LIS2.App.Tests;

public sealed class SettingsStoreTests
{
    [Fact]
    public void Load_MissingFile_ReturnsNormalizedDefaults()
    {
        using var scope = new TempSettingsScope();
        var settings = scope.Store.Load();

        Assert.Equal("Virtual", settings.TransportMode);
        Assert.Equal(100, settings.BrightnessPercent);
        Assert.Equal(3, settings.Pages.Count);
        Assert.Equal(8, settings.CustomGlyphs.Count);
        Assert.Equal(4, settings.Fans.Channels.Length);
    }

    [Fact]
    public void Load_PartialLegacyJson_FillsMissingCollectionsAndClampsValues()
    {
        using var scope = new TempSettingsScope();
        File.WriteAllText(
            scope.Path,
            """
            {
              "TransportMode": "broken",
              "BrightnessPercent": 12,
              "ThemeMode": "NoSuchTheme",
              "Pages": null,
              "CustomGlyphs": null,
              "HardwareSensorPreferences": null,
              "HomeAssistant": null,
              "Fans": {
                "Channels": null
              }
            }
            """);

        var settings = scope.Store.Load();

        Assert.Equal("Virtual", settings.TransportMode);
        Assert.Equal(100, settings.BrightnessPercent);
        Assert.Equal(nameof(AppThemeMode.System), settings.ThemeMode);
        Assert.Equal(3, settings.Pages.Count);
        Assert.Equal(8, settings.CustomGlyphs.Count);
        Assert.NotNull(settings.HardwareSensorPreferences);
        Assert.NotNull(settings.HomeAssistant);
        Assert.Equal(4, settings.Fans.Channels.Length);
    }

    [Fact]
    public void Load_InvalidJson_QuarantinesFileAndReturnsDefaults()
    {
        using var scope = new TempSettingsScope();
        File.WriteAllText(scope.Path, "{ definitely not json");

        var settings = scope.Store.Load();

        Assert.Equal(3, settings.Pages.Count);
        Assert.False(File.Exists(scope.Path));
        Assert.Single(Directory.GetFiles(scope.Directory, "settings.corrupt-*.json"));
    }

    [Fact]
    public async Task Save_HomeAssistantToken_IsProtectedAtRestAndRoundTrips()
    {
        using var scope = new TempSettingsScope();
        const string token = "super-secret-home-assistant-token";

        var settings = new AppSettings();
        settings.HomeAssistant.Enabled = true;
        settings.HomeAssistant.Url = "https://ha.example.test";
        settings.HomeAssistant.AccessToken = token;

        await scope.Store.SaveAsync(settings);

        var json = File.ReadAllText(scope.Path);
        Assert.DoesNotContain(token, json, StringComparison.Ordinal);
        Assert.Contains("\"AccessToken\": \"dpapi:v1:", json, StringComparison.Ordinal);

        var loaded = await scope.Store.LoadAsync();
        Assert.Equal(token, loaded.HomeAssistant.AccessToken);
    }

    [Fact]
    public async Task LegacyPlaintextHomeAssistantToken_IsMigratedOnNextSave()
    {
        using var scope = new TempSettingsScope();
        const string token = "legacy-plaintext-token";

        File.WriteAllText(
            scope.Path,
            $"""
            {
              "HomeAssistant": {
                "Enabled": true,
                "Url": "https://ha.example.test",
                "AccessToken": "{{token}}",
                "EntityPreferences": []
              }
            }
            """);

        var loaded = scope.Store.Load();
        Assert.Equal(token, loaded.HomeAssistant.AccessToken);

        await scope.Store.SaveAsync(loaded);

        var migratedJson = File.ReadAllText(scope.Path);
        Assert.DoesNotContain(token, migratedJson, StringComparison.Ordinal);
        Assert.Contains("\"AccessToken\": \"dpapi:v1:", migratedJson, StringComparison.Ordinal);

        var reloaded = scope.Store.Load();
        Assert.Equal(token, reloaded.HomeAssistant.AccessToken);
    }

    [Fact]
    public async Task InvalidProtectedHomeAssistantToken_IsNeverUsedAsPlaintext()
    {
        using var scope = new TempSettingsScope();

        File.WriteAllText(
            scope.Path,
            """
            {
              "HomeAssistant": {
                "Enabled": true,
                "Url": "https://ha.example.test",
                "AccessToken": "dpapi:v1:not-valid-base64",
                "EntityPreferences": []
              }
            }
            """);

        var loaded = await scope.Store.LoadAsync();

        Assert.Equal(string.Empty, loaded.HomeAssistant.AccessToken);
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsNormalizedSettings()
    {
        using var scope = new TempSettingsScope();
        var settings = new AppSettings
        {
            TransportMode = "serial",
            BrightnessPercent = 75,
            Pages =
            [
                new PageDefinition
                {
                    Id = "",
                    Name = "",
                    Line1Template = null!,
                    Line2Template = null!,
                    DurationSeconds = 0,
                    Line1OverflowMode = "invalid",
                    Line2OverflowMode = "marquee",
                    ScrollStepMilliseconds = 1,
                    ScrollEdgePauseMilliseconds = -5
                }
            ]
        };

        await scope.Store.SaveAsync(settings);
        var loaded = await scope.Store.LoadAsync();

        Assert.Equal("Serial", loaded.TransportMode);
        Assert.Equal(75, loaded.BrightnessPercent);
        Assert.Single(loaded.Pages);
        Assert.False(string.IsNullOrWhiteSpace(loaded.Pages[0].Id));
        Assert.Equal("Page", loaded.Pages[0].Name);
        Assert.Equal(string.Empty, loaded.Pages[0].Line1Template);
        Assert.Equal(1, loaded.Pages[0].DurationSeconds);
        Assert.Equal("PingPong", loaded.Pages[0].Line1OverflowMode);
        Assert.Equal("Marquee", loaded.Pages[0].Line2OverflowMode);
        Assert.Equal(50, loaded.Pages[0].ScrollStepMilliseconds);
        Assert.Equal(0, loaded.Pages[0].ScrollEdgePauseMilliseconds);
    }

    private sealed class TempSettingsScope : IDisposable
    {
        public TempSettingsScope()
        {
            Directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "LIS2ControlCenter.Tests",
                Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(Directory);
            Path = System.IO.Path.Combine(Directory, "settings.json");
            Store = new SettingsStore(Path);
        }

        public string Directory { get; }
        public string Path { get; }
        public SettingsStore Store { get; }

        public void Dispose()
        {
            try
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }
}