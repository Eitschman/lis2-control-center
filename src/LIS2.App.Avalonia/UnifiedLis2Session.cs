using LIS2.Core;
using LIS2.Runtime;

namespace LIS2.App.Avalonia;

/// <summary>
/// Single-owner device session for display and fan commands. Hardware fan output is
/// explicitly authorized and the safe frame is sent before ANY display command.
/// Reconnection requires a new session and fresh authorization.
/// </summary>
public sealed class UnifiedLis2Session : IFanOutputSink, IAsyncDisposable
{
    private readonly Lis2Device _device;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _authorized;
    private int[]? _safeFrame;
    private bool _disposed;

    public UnifiedLis2Session(ILis2Transport transport) =>
        _device = new Lis2Device(transport ?? throw new ArgumentNullException(nameof(transport)));

    public bool IsConnected => _device.IsConnected;
    public bool FansAuthorized => _authorized && IsConnected;

    public async Task ConnectDisplayOnlyAsync(CancellationToken token = default)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsConnected) throw new InvalidOperationException("Already connected.");
            _authorized = false;
            _safeFrame = null;
            await _device.ConnectAsync(token).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task ConnectWithFanSafetyAsync(IReadOnlyList<int> safeFrame, CancellationToken token = default)
    {
        Validate(safeFrame);
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsConnected) throw new InvalidOperationException("Already connected.");
            _authorized = false;
            _safeFrame = null;
            await _device.ConnectAsync(token).ConfigureAwait(false);
            try
            {
                await _device.SetFansAsync(safeFrame[0], safeFrame[1], safeFrame[2], safeFrame[3], token).ConfigureAwait(false);
                _safeFrame = safeFrame.ToArray();
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

    /// <summary>Explicitly authorize fans on an already-open display connection.
    /// The safe frame is the first fan command; display writes share the same gate.</summary>
    public async Task AuthorizeConnectedFansAsync(IReadOnlyList<int> safeFrame, CancellationToken token = default)
    {
        Validate(safeFrame);
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            RequireConnected();
            if (_authorized || _safeFrame is not null)
                throw new InvalidOperationException("Fan output already authorized or requires reconnect.");
            // Keep the safety frame even when the initial write fails: the transport
            // may have accepted bytes before reporting an I/O error.
            _safeFrame = safeFrame.ToArray();
            await _device.SetFansAsync(safeFrame[0], safeFrame[1], safeFrame[2], safeFrame[3], token).ConfigureAwait(false);
            _authorized = true;
        }
        finally { _gate.Release(); }
    }

    /// <summary>Revoke authorization while retaining the display connection.</summary>
    public async Task RevokeFansAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var safe = IsConnected ? _safeFrame : null;
            _authorized = false;
            // Retain the frame if restoration fails; disconnect/disposal must retry.
            if (safe is null) { _safeFrame = null; return; }
            await _device.SetFansAsync(safe[0], safe[1], safe[2], safe[3], CancellationToken.None).ConfigureAwait(false);
            _safeFrame = null;
        }
        finally { _gate.Release(); }
    }

    public async Task WriteAsync(IReadOnlyList<int> outputs, CancellationToken token)
    {
        Validate(outputs);
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (!FansAuthorized) throw new InvalidOperationException("Fan output is not authorized.");
            try { await _device.SetFansAsync(outputs[0], outputs[1], outputs[2], outputs[3], token).ConfigureAwait(false); }
            catch { _authorized = false; throw; }
        }
        finally { _gate.Release(); }
    }

    public async Task RenderBothAsync(string line1, string line2, CancellationToken token = default)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (!IsConnected) throw new InvalidOperationException("Display is disconnected.");
            await _device.WriteLineAsync(1, line1, token).ConfigureAwait(false);
            await _device.WriteLineAsync(2, line2, token).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }


    public async Task WriteLineAsync(int line, string text, CancellationToken token = default)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try { RequireConnected(); await _device.WriteLineAsync(line, text, token).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    public async Task ClearAsync(CancellationToken token = default)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try { RequireConnected(); await _device.ClearAsync(token).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    public async Task SetBrightnessAsync(Lis2Brightness brightness, CancellationToken token = default)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try { RequireConnected(); await _device.SetBrightnessAsync(brightness, token).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    public async Task ProgramGlyphAsync(int slot, byte[] rows, CancellationToken token = default)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try { RequireConnected(); await _device.ProgramCharacterAsync(slot, rows, token).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    public async Task ProgramAllGlyphsAsync(IReadOnlyList<CustomGlyphSettings> glyphs, CancellationToken token = default)
    {
        if (glyphs is null || glyphs.Count != 8 || glyphs.Any(g => g.Rows is null || g.Rows.Length != 8 || g.Rows.Any(row => row > 31)))
            throw new ArgumentException("Exactly eight valid 5x8 glyphs are required.", nameof(glyphs));
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            RequireConnected();
            for (var slot = 1; slot <= 8; slot++)
                await _device.ProgramCharacterAsync(slot, glyphs[slot - 1].Rows, token).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task WriteRawDisplayBytesAsync(int line, int column, byte[] bytes, CancellationToken token = default)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try { RequireConnected(); await _device.WriteRawDisplayBytesAsync(line, column, bytes, token).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    private void RequireConnected()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsConnected) throw new InvalidOperationException("LIS2 device is disconnected.");
    }

    public async Task DisconnectAsync(CancellationToken token = default)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var safe = IsConnected ? _safeFrame : null;
            _authorized = false;
            _safeFrame = null;
            try
            {
                if (safe is not null)
                    await _device.SetFansAsync(safe[0], safe[1], safe[2], safe[3], CancellationToken.None).ConfigureAwait(false);
            }
            finally { await _device.DisconnectAsync(CancellationToken.None).ConfigureAwait(false); }
        }
        finally { _gate.Release(); }
    }

    private static void Validate(IReadOnlyList<int> values)
    {
        if (values is null || values.Count != 4 || values.Any(x => x is < 0 or > 100))
            throw new ArgumentException("Exactly four fan percentages from 0 to 100 are required.", nameof(values));
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed) return;
            _disposed = true;
            var safe = IsConnected ? _safeFrame : null;
            _authorized = false;
            _safeFrame = null;
            try
            {
                if (safe is not null)
                    await _device.SetFansAsync(safe[0], safe[1], safe[2], safe[3], CancellationToken.None).ConfigureAwait(false);
            }
            finally { await _device.DisposeAsync().ConfigureAwait(false); }
        }
        finally { _gate.Release(); }
    }
}
