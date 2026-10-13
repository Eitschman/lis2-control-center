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

    public SerialDisplaySession(string portName) : this(new SerialLis2Transport(portName)) { }

    public SerialDisplaySession(ILis2Transport transport)
    {
        _device = new Lis2Device(transport);
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

    public async Task RenderBothAsync(string line1, string line2, CancellationToken cancellationToken = default)
    {
        await _device.WriteLineAsync(1, line1, cancellationToken).ConfigureAwait(false);
        await _device.WriteLineAsync(2, line2, cancellationToken).ConfigureAwait(false);
    }

    public Task ProgramGlyphAsync(int slot, byte[] rows, CancellationToken cancellationToken = default) =>
        _device.ProgramCharacterAsync(slot, rows, cancellationToken);

    public async Task ProgramAllGlyphsAsync(IReadOnlyList<CustomGlyphSettings> glyphs, CancellationToken cancellationToken = default)
    {
        if (glyphs.Count != 8 || glyphs.Any(g => g.Rows is null || g.Rows.Length != 8 || g.Rows.Any(row => row > 31)))
            throw new ArgumentException("Exactly eight valid 5x8 glyphs are required.", nameof(glyphs));
        for (var slot = 1; slot <= 8; slot++)
            await _device.ProgramCharacterAsync(slot, glyphs[slot - 1].Rows, cancellationToken).ConfigureAwait(false);
    }

    public Task WriteRawDisplayBytesAsync(int line, int column, byte[] bytes, CancellationToken cancellationToken = default) =>
        _device.WriteRawDisplayBytesAsync(line, column, bytes, cancellationToken);

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
