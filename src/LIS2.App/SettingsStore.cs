using System.Text.Json;

namespace LIS2.App;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;

    public SettingsStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LIS2ControlCenter");
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "settings.json");
    }

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_path))
            return new AppSettings();

        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions)
            ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions);
    }
}
