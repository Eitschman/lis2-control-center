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
    private bool _fanTransition;
    public bool FansRunning => _fanLoop is not null;
    public Exception? FanFault => _fanLoop?.LastFault;
    public SerialDisplaySession(string portName) : this(new SerialLis2Transport(portName)) { }
    public SerialDisplaySession(ILis2Transport transport) => _session = new UnifiedLis2Session(transport);
    public bool IsConnected => _session.IsConnected;
    public Task ConnectAsync(CancellationToken cancellationToken = default) => _session.ConnectDisplayOnlyAsync(cancellationToken);
    public async Task StartFansAsync(IFanSensorSource sensors, IReadOnlyList<FanChannelConfiguration> channels)
    {
        if (_fanTransition || _fanLoop is not null) throw new InvalidOperationException("Fan control already active.");
        _fanTransition = true;
        try
        {
            var runtime = new FanRuntime(sensors, _session, channels);
            var loop = new FanRuntimeLoop(runtime);
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
                await loop.DisposeAsync();
                await runtime.DisposeAsync();
                await _session.RevokeFansAsync();
                throw;
            }
        }
        finally { _fanTransition = false; }
    }

    public async Task StopFansAsync()
    {
        if (_fanTransition) throw new InvalidOperationException("Fan transition in progress.");
        _fanTransition = true;
        try
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
                        await loop.DisposeAsync();
                        if (runtime is not null) await runtime.DisposeAsync();
                        _fanLoop = null;
                        _fanRuntime = null;
                    }
                }
            }
        }
        finally { _fanTransition = false; }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try { await StopFansAsync(); }
        finally { await _session.DisconnectAsync(cancellationToken); }
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
        try { await StopFansAsync(); }
        finally { await _session.DisposeAsync(); }
    }
}
