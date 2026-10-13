using LIS2.Core;
using LIS2.Runtime;
using LIS2.Fans;

namespace LIS2.App.Avalonia;

/// <summary>Display facade backed by the unified serial command gate. Display-only access never authorizes fans.</summary>
public sealed class SerialDisplaySession : IAsyncDisposable
{
    private readonly UnifiedLis2Session _session;
    private FanRuntime? _fanRuntime;
    private FanRuntimeLoop? _fanLoop;
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private bool _fanTransition;
    private bool _disposed;
    private bool _disconnected;
    private volatile bool _closing;
    private Exception? _lastFanFault;
    public bool FansRunning => _fanLoop is not null;
    public bool FansBusy => _fanTransition;
    public Exception? FanFault => _fanLoop?.LastFault ?? _lastFanFault;
    public event Action<Exception>? FanFaulted;
    public SerialDisplaySession(string portName) : this(new SerialLis2Transport(portName)) { }
    public SerialDisplaySession(ILis2Transport transport) => _session = new UnifiedLis2Session(transport);
    public bool IsConnected => _session.IsConnected;
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_disconnected || _closing) throw new InvalidOperationException("Session is closing or disconnected.");
            await _session.ConnectDisplayOnlyAsync(cancellationToken);
        }
        finally { _lifecycle.Release(); }
    }
    public async Task StartFansAsync(IFanSensorSource sensors, IReadOnlyList<FanChannelConfiguration> channels)
    {
        await _lifecycle.WaitAsync();
        _fanTransition = true;
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_disconnected || _closing) throw new InvalidOperationException("Session is closing or disconnected.");
            if (_fanLoop is not null) throw new InvalidOperationException("Fan control already active.");
            _lastFanFault = null;
            var runtime = new FanRuntime(sensors, _session, channels);
            var loop = new FanRuntimeLoop(runtime, onFault: _ => _session.RevokeFansAsync());
            loop.Faulted += ex =>
            {
                _lastFanFault = ex;
                FanFaulted?.Invoke(ex);
            };
            try
            {
                await _session.AuthorizeConnectedFansAsync(new[] { 100, 100, 100, 100 });
                await runtime.SetOutputEnabledAsync(true);
                await runtime.TickAsync();
                if (!loop.Start()) throw new InvalidOperationException("Fan loop failed to start.");
                _fanRuntime = runtime;
                _fanLoop = loop;
            }
            catch
            {
                try { await loop.DisposeAsync(); }
                finally
                {
                    try { await runtime.DisposeAsync(); }
                    finally { await _session.RevokeFansAsync(); }
                }
                throw;
            }
        }
        finally { _fanTransition = false; _lifecycle.Release(); }
    }

    public async Task StopFansAsync()
    {
        await _lifecycle.WaitAsync();
        _fanTransition = true;
        try { await StopFansCoreAsync(); }
        finally { _fanTransition = false; _lifecycle.Release(); }
    }

    private async Task StopFansCoreAsync()
    {
        var loop = _fanLoop;
        var runtime = _fanRuntime;
        if (loop is null) return;
        try { await loop.StopAndDrainAsync(); }
        finally
        {
            try { if (runtime is not null) await runtime.SetOutputEnabledAsync(false); }
            finally
            {
                try { await _session.RevokeFansAsync(); }
                finally
                {
                    try
                    {
                        await loop.DisposeAsync();
                        if (runtime is not null) await runtime.DisposeAsync();
                    }
                    finally
                    {
                        _fanLoop = null;
                        _fanRuntime = null;
                    }
                }
            }
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _closing = true;
        await _lifecycle.WaitAsync();
        _fanTransition = true;
        try
        {
            _disconnected = true;
            try { await StopFansCoreAsync(); }
            finally { await _session.DisconnectAsync(CancellationToken.None); }
        }
        finally { _fanTransition = false; _lifecycle.Release(); }
    }
    public Task WriteLineAsync(int line, string text, CancellationToken cancellationToken = default) => _session.WriteLineAsync(line, text, cancellationToken);
    public Task RenderBothAsync(string line1, string line2, CancellationToken cancellationToken = default) => _session.RenderBothAsync(line1, line2, cancellationToken);
    public Task ProgramGlyphAsync(int slot, byte[] rows, CancellationToken cancellationToken = default) => _session.ProgramGlyphAsync(slot, rows, cancellationToken);
    public Task ProgramAllGlyphsAsync(IReadOnlyList<CustomGlyphSettings> glyphs, CancellationToken cancellationToken = default) => _session.ProgramAllGlyphsAsync(glyphs, cancellationToken);
    public Task WriteRawDisplayBytesAsync(int line, int column, byte[] bytes, CancellationToken cancellationToken = default) => _session.WriteRawDisplayBytesAsync(line, column, bytes, cancellationToken);
    public Task ClearAsync(CancellationToken cancellationToken = default) => _session.ClearAsync(cancellationToken);
    public Task SetBrightnessAsync(Lis2Brightness brightness, CancellationToken cancellationToken = default) => _session.SetBrightnessAsync(brightness, cancellationToken);
    public async ValueTask DisposeAsync()
    {
        _closing = true;
        await _lifecycle.WaitAsync();
        _fanTransition = true;
        try
        {
            if (_disposed) return;
            _disposed = true;
            _disconnected = true;
            try { await StopFansCoreAsync(); }
            finally { await _session.DisposeAsync(); }
        }
        finally { _fanTransition = false; _lifecycle.Release(); }
    }
}
