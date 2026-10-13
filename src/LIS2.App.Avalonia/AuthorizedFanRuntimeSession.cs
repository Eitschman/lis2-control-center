using LIS2.Core;
using LIS2.Runtime;

namespace LIS2.App.Avalonia;

/// <summary>
/// Explicit opt-in hardware fan runtime. The first serial command is the safe
/// fan frame; the periodic loop must be drained before the device is closed.
/// UI wiring deliberately remains disabled until hardware safety review.
/// </summary>
public sealed class AuthorizedFanRuntimeSession : IAsyncDisposable
{
    private readonly UnifiedLis2Session _device;
    private readonly FanRuntime _runtime;
    private readonly FanRuntimeLoop _loop;
    private bool _started;
    private bool _disposed;

    public AuthorizedFanRuntimeSession(ILis2Transport transport, IFanSensorSource sensors,
        IReadOnlyList<LIS2.Fans.FanChannelConfiguration> channels, TimeSpan? interval = null)
    {
        _device = new UnifiedLis2Session(transport);
        _runtime = new FanRuntime(sensors, _device, channels);
        _loop = new FanRuntimeLoop(_runtime, interval);
    }

    public bool IsConnected => _device.IsConnected;
    public bool FansAuthorized => _device.FansAuthorized;
    public Exception? LastFault => _loop.LastFault;
    public event Action<Exception>? Faulted
    {
        add => _loop.Faulted += value;
        remove => _loop.Faulted -= value;
    }

    public async Task StartAsync(IReadOnlyList<int> safeFrame, CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started || _device.IsConnected) throw new InvalidOperationException("Fan session already started.");
        await _device.ConnectWithFanSafetyAsync(safeFrame, token).ConfigureAwait(false);
        try
        {
            // Force the first calculated state to be written before periodic updates.
            await _runtime.SetOutputEnabledAsync(true, token).ConfigureAwait(false);
            await _runtime.TickAsync(token).ConfigureAwait(false);
            if (!_loop.Start()) throw new InvalidOperationException("Fan loop already running.");
            _started = true;
        }
        catch
        {
            await _runtime.SetOutputEnabledAsync(false, CancellationToken.None).ConfigureAwait(false);
            await _device.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task StopAsync()
    {
        try { await _loop.StopAndDrainAsync().ConfigureAwait(false); }
        finally
        {
            try { await _runtime.SetOutputEnabledAsync(false, CancellationToken.None).ConfigureAwait(false); }
            finally
            {
                _started = false;
                if (_device.IsConnected) await _device.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await StopAsync().ConfigureAwait(false); }
        finally
        {
            await _loop.DisposeAsync().ConfigureAwait(false);
            await _runtime.DisposeAsync().ConfigureAwait(false);
            await _device.DisposeAsync().ConfigureAwait(false);
        }
    }
}
