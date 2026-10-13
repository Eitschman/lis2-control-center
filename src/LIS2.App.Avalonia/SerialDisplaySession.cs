using LIS2.Core;

namespace LIS2.App.Avalonia;

/// <summary>
/// Explicit opt-in display-only serial session. Never issues fan commands.
/// Fan-safe startup and reconnect require a separately reviewed runtime.
/// </summary>
public sealed class SerialDisplaySession : IAsyncDisposable
{
    private readonly Lis2Device _device;
    private readonly SemaphoreSlim _lifecycle = new(1, 1);

    public SerialDisplaySession(string portName)
    {
        _device = new Lis2Device(new SerialLis2Transport(portName));
    }

    public bool IsConnected => _device.IsConnected;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsConnected) return;
            // No automatic reconnect: device unplug requires explicit operator action.
            await _device.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        finally { _lifecycle.Release(); }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { await _device.DisconnectAsync(cancellationToken).ConfigureAwait(false); }
        finally { _lifecycle.Release(); }
    }

    public Task WriteLineAsync(int line, string text, CancellationToken cancellationToken = default) =>
        _device.WriteLineAsync(line, text, cancellationToken);

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        _device.ClearAsync(cancellationToken);

    public Task SetBrightnessAsync(Lis2Brightness brightness, CancellationToken cancellationToken = default) =>
        _device.SetBrightnessAsync(brightness, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try { await _device.DisposeAsync().ConfigureAwait(false); }
        finally { _lifecycle.Release(); _lifecycle.Dispose(); }
    }
}
