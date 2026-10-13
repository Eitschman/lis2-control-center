using LIS2.Core;
using LIS2.Runtime;

namespace LIS2.App.Avalonia;

/// <summary>
/// Explicitly authorized serial fan session. Safety frame is the FIRST command
/// after opening transport; display access is impossible before successful initialization.
/// No automatic reconnect. A failed write revokes authorization.
/// </summary>
public sealed class SafeSerialFanSession : IFanOutputSink, IAsyncDisposable
{
    private readonly Lis2Device _device;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _authorized;
    private bool _disposed;

    public SafeSerialFanSession(ILis2Transport transport) =>
        _device = new Lis2Device(transport ?? throw new ArgumentNullException(nameof(transport)));

    public bool IsAuthorized => _authorized && _device.IsConnected;

    public async Task ConnectAndAuthorizeAsync(IReadOnlyList<int> safeOutputs, CancellationToken token = default)
    {
        Validate(safeOutputs);
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_device.IsConnected) throw new InvalidOperationException("Already connected; disconnect first.");
            _authorized = false;
            await _device.ConnectAsync(token).ConfigureAwait(false);
            try
            {
                await _device.SetFansAsync(safeOutputs[0], safeOutputs[1], safeOutputs[2], safeOutputs[3], token).ConfigureAwait(false);
                _authorized = true;
            }
            catch
            {
                await _device.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }
        finally { _gate.Release(); }
    }

    public async Task WriteAsync(IReadOnlyList<int> outputs, CancellationToken token)
    {
        Validate(outputs);
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (!IsAuthorized) throw new InvalidOperationException("Fan hardware is not authorized.");
            try { await _device.SetFansAsync(outputs[0], outputs[1], outputs[2], outputs[3], token).ConfigureAwait(false); }
            catch { _authorized = false; throw; }
        }
        finally { _gate.Release(); }
    }

    public async Task DisconnectAsync(CancellationToken token = default)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            _authorized = false;
            await _device.DisconnectAsync(token).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    private static void Validate(IReadOnlyList<int> values)
    {
        if (values is null || values.Count != 4 || values.Any(x => x is < 0 or > 100))
            throw new ArgumentException("Exactly four valid fan percentages required.", nameof(values));
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed) return;
            _disposed = true;
            _authorized = false;
            await _device.DisposeAsync().ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }
}
