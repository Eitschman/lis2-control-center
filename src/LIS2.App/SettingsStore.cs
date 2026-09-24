using System.IO;
using System.Text.Json;

namespace LIS2.App;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public SettingsStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LIS2ControlCenter");
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_path))
            return new AppSettings();

        using var stream = File.OpenRead(_path);
        return JsonSerializer.Deserialize<AppSettings>(stream, JsonOptions)
            ?? new AppSettings();
    }

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_path))
            return new AppSettings();

        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions)
            .ConfigureAwait(false)
            ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _saveLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var tempPath = _path + ".tmp";

            await using (var stream = new FileStream(
                tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, settings, JsonOptions)
                    .ConfigureAwait(false);
            }

            File.Move(tempPath, _path, overwrite: true);
        }
        finally
        {
            _saveLock.Release();
        }
    }
}
