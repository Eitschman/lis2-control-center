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

    public SettingsStore(string? path = null)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            _path = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            return;
        }

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LIS2ControlCenter");
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_path))
            return CreateDefaults();

        try
        {
            using var stream = File.OpenRead(_path);
            return Normalize(JsonSerializer.Deserialize<AppSettings>(stream, JsonOptions));
        }
        catch (JsonException)
        {
            QuarantineInvalidSettings();
            return CreateDefaults();
        }
    }

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_path))
            return CreateDefaults();

        try
        {
            await using var stream = File.OpenRead(_path);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions)
                .ConfigureAwait(false);
            return Normalize(settings);
        }
        catch (JsonException)
        {
            QuarantineInvalidSettings();
            return CreateDefaults();
        }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.EnsureDefaults();

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

    private static AppSettings CreateDefaults()
    {
        var settings = new AppSettings();
        settings.EnsureDefaults();
        return settings;
    }

    private static AppSettings Normalize(AppSettings? settings)
    {
        settings ??= new AppSettings();
        settings.EnsureDefaults();
        return settings;
    }

    private void QuarantineInvalidSettings()
    {
        try
        {
            if (!File.Exists(_path))
                return;

            var directory = Path.GetDirectoryName(_path) ?? string.Empty;
            var fileName = Path.GetFileNameWithoutExtension(_path);
            var extension = Path.GetExtension(_path);
            var quarantinePath = Path.Combine(
                directory,
                $"{fileName}.corrupt-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}{extension}");

            File.Move(_path, quarantinePath, overwrite: false);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}