using System.Reflection;

namespace LIS2.App.Avalonia;

/// <summary>Lädt den GSMTC-Anbieter ohne Windows-TFM-Abhängigkeit in der gemeinsamen Avalonia-App.</summary>
public sealed class WindowsMediaBridge : IAsyncDisposable
{
    private object? _provider;
    private MethodInfo? _stop;
    private MethodInfo? _dispose;
    private PropertyInfo? _values;
    private bool _started;

    public async Task StartAsync(string? providerFile = null)
    {
        if (!OperatingSystem.IsWindows()) return;
        if (_provider is not null) throw new InvalidOperationException("Windows Media provider is already started.");
        var file = providerFile ?? Path.Combine(AppContext.BaseDirectory, "LIS2.WindowsMedia.dll");
        if (!File.Exists(file)) throw new FileNotFoundException("Windows Media provider is missing.", file);
        var assembly = Assembly.LoadFrom(file);
        var type = assembly.GetType("LIS2.WindowsMedia.WindowsMediaDataSource");
        if (type is null) throw new TypeLoadException("Windows Media provider type is missing.");
        _provider = Activator.CreateInstance(type, [null]);
        if (_provider is null) throw new InvalidOperationException("Could not create Windows Media provider.");
        try
        {
            _values = type.GetProperty("Values") ?? throw new MissingMemberException(type.FullName, "Values");
            _stop = type.GetMethod("StopAsync") ?? throw new MissingMethodException(type.FullName, "StopAsync");
            _dispose = type.GetMethod("DisposeAsync") ?? throw new MissingMethodException(type.FullName, "DisposeAsync");
            var start = type.GetMethod("StartAsync") ?? throw new MissingMethodException(type.FullName, "StartAsync");
            if (start.Invoke(_provider, [CancellationToken.None]) is not Task task)
                throw new InvalidOperationException("Invalid Windows Media startup contract.");
            await task;
            _started = true;
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public bool IsAvailable => _started;

    public IReadOnlyDictionary<string, object?> Values
    {
        get
        {
            if (!_started || _provider is null || _values is null)
                return new Dictionary<string, object?> { ["Connected"] = false };
            // Anbieterfehler müssen die sichtbare Fehleranzeige erreichen und dürfen nicht wie ein fehlender Player aussehen.
            return _values.GetValue(_provider) as IReadOnlyDictionary<string, object?>
                ?? throw new InvalidOperationException("Invalid Windows Media snapshot contract.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_provider is null) return;
        try
        {
            await LIS2.Runtime.RuntimeShutdown.RunAsync(
                () => _stop?.Invoke(_provider, [CancellationToken.None]) as Task ?? Task.CompletedTask,
                () => _dispose?.Invoke(_provider, null) is ValueTask dispose ? dispose.AsTask() : Task.CompletedTask);
        }
        finally { _provider = null; _started = false; _values = null; _stop = null; _dispose = null; }
    }
}
