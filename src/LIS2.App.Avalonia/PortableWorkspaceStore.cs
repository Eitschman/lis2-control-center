using System.Text.Json;

namespace LIS2.App.Avalonia;

/// <summary>Portable, non-secret workspace persistence; never serializes HA access tokens.</summary>
public sealed class PortableWorkspaceStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public PortableWorkspaceStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LIS2ControlCenter", "avalonia-workspace.json");
    }

    public bool Exists => File.Exists(_path);

    public PortableWorkspace Load()
    {
        if (!File.Exists(_path)) return new PortableWorkspace();
        try
        {
            var result = JsonSerializer.Deserialize<PortableWorkspace>(File.ReadAllText(_path), Options)
                ?? new PortableWorkspace();
            result.Pages ??= new List<PageDefinition>();
            result.CustomGlyphs ??= new List<CustomGlyphSettings>();
            result.HomeAssistantEntityPreferences ??= new List<PortableHaEntityPreference>();
            result.HardwareSensorPreferences ??= new List<PortableHardwareSensorPreference>();
            result.PageVariableFavorites ??= new List<string>();
            result.HomeAssistantUrl ??= string.Empty;
            result.Fans = PortableFanSettings.Normalize(result.Fans);
            return result;
        }
        catch (JsonException)
        {
            var backup = _path + ".corrupt-" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");
            File.Move(_path, backup);
            return new PortableWorkspace();
        }
    }

    public async Task SaveAsync(PortableWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        await _saveLock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var temp = _path + ".tmp";
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(workspace, Options));
            File.Move(temp, _path, overwrite: true);
        }
        finally { _saveLock.Release(); }
    }
}

public sealed class PortableWorkspace
{
    public List<PageDefinition> Pages { get; set; } = new();
    public PortableFanSettings Fans { get; set; } = new();
    public string HomeAssistantUrl { get; set; } = string.Empty;
    public List<CustomGlyphSettings> CustomGlyphs { get; set; } = new();
    public List<PortableHaEntityPreference> HomeAssistantEntityPreferences { get; set; } = new();
    public List<PortableHardwareSensorPreference> HardwareSensorPreferences { get; set; } = new();
    public List<string> PageVariableFavorites { get; set; } = new();
    public int BrightnessPercent { get; set; } = 100;
    public bool ProgramCustomGlyphsOnConnect { get; set; }
    public string ThemeMode { get; set; } = "System";
    public string LanguageMode { get; set; } = "System";
}

public sealed class PortableHardwareSensorPreference
{
    public string Key { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
}
