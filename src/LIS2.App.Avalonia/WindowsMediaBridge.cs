using System.Reflection;

namespace LIS2.App.Avalonia;

/// <summary>Loads the existing Windows-only GSMTC provider without a Windows TFM dependency in the shared Avalonia app.</summary>
public sealed class WindowsMediaBridge : IAsyncDisposable
{
    private object? _provider;
    private MethodInfo? _stop;
    private MethodInfo? _dispose;
    private PropertyInfo? _values;

    public async Task StartAsync()
    {
        if (!OperatingSystem.IsWindows()) return;
        var file = Path.Combine(AppContext.BaseDirectory, "LIS2.WindowsMedia.dll");
        if (!File.Exists(file)) return;
        var assembly = Assembly.LoadFrom(file);
        var type = assembly.GetType("LIS2.WindowsMedia.WindowsMediaDataSource");
        if (type is null) return;
        _provider = Activator.CreateInstance(type, [null]);
        if (_provider is null) return;
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
        if (_stop?.Invoke(_provider, [CancellationToken.None]) is Task stop) await stop;
        if (_dispose?.Invoke(_provider, null) is ValueTask dispose) await dispose;
        _provider = null;
    }
}
