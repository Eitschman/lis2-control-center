using System.Reflection;

namespace LIS2.App.Avalonia;

/// <summary>Loads the existing Windows-only GSMTC provider without a Windows TFM dependency in the shared Avalonia app.</summary>
public sealed class WindowsMediaBridge : IAsyncDisposable
{
    private object? _provider;
    private MethodInfo? _stop;
    private MethodInfo? _dispose;
    private PropertyInfo? _values;

    public async Task StartAsync(string? providerFile = null)
    {
        if (!OperatingSystem.IsWindows()) return;
        var file = providerFile ?? Path.Combine(AppContext.BaseDirectory, "LIS2.WindowsMedia.dll");
        if (!File.Exists(file)) throw new FileNotFoundException("Windows Media provider is missing.", file);
        var assembly = Assembly.LoadFrom(file);
        var type = assembly.GetType("LIS2.WindowsMedia.WindowsMediaDataSource");
        if (type is null) throw new TypeLoadException("Windows Media provider type is missing.");
        _provider = Activator.CreateInstance(type, [null]);
        if (_provider is null) throw new InvalidOperationException("Could not create Windows Media provider.");
        _values = type.GetProperty("Values");
        _stop = type.GetMethod("StopAsync");
        _dispose = type.GetMethod("DisposeAsync");
        var start = type.GetMethod("StartAsync");
        if (start?.Invoke(_provider, [CancellationToken.None]) is Task task)
            await task;
    }

    public bool IsAvailable => _provider is not null;

    public IReadOnlyDictionary<string, object?> Values
    {
        get
        {
            if (_provider is null || _values is null)
                return new Dictionary<string, object?> { ["Connected"] = false };
            try
            {
                if (_values.GetValue(_provider) is IReadOnlyDictionary<string, object?> values)
                    return values;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Windows GSMTC snapshot failed: {0}", ex);
            }
            return new Dictionary<string, object?> { ["Connected"] = false };
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
        finally { _provider = null; }
    }
}
