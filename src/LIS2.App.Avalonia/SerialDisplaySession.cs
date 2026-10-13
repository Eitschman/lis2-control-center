using LIS2.Core;

namespace LIS2.App.Avalonia;

/// <summary>Display facade backed by the unified serial command gate. Display-only access never authorizes fans.</summary>
public sealed class SerialDisplaySession : IAsyncDisposable
{
    private readonly UnifiedLis2Session _session;
    public SerialDisplaySession(string portName) : this(new SerialLis2Transport(portName)) { }
    public SerialDisplaySession(ILis2Transport transport) => _session = new UnifiedLis2Session(transport);
    public bool IsConnected => _session.IsConnected;
    public Task ConnectAsync(CancellationToken cancellationToken = default) => _session.ConnectDisplayOnlyAsync(cancellationToken);
    public Task DisconnectAsync(CancellationToken cancellationToken = default) => _session.DisconnectAsync(cancellationToken);
    public Task WriteLineAsync(int line, string text, CancellationToken cancellationToken = default) => _session.WriteLineAsync(line, text, cancellationToken);
    public Task RenderBothAsync(string line1, string line2, CancellationToken cancellationToken = default) => _session.RenderBothAsync(line1, line2, cancellationToken);
    public Task ProgramGlyphAsync(int slot, byte[] rows, CancellationToken cancellationToken = default) => _session.ProgramGlyphAsync(slot, rows, cancellationToken);
    public Task ProgramAllGlyphsAsync(IReadOnlyList<CustomGlyphSettings> glyphs, CancellationToken cancellationToken = default) => _session.ProgramAllGlyphsAsync(glyphs, cancellationToken);
    public Task WriteRawDisplayBytesAsync(int line, int column, byte[] bytes, CancellationToken cancellationToken = default) => _session.WriteRawDisplayBytesAsync(line, column, bytes, cancellationToken);
    public Task ClearAsync(CancellationToken cancellationToken = default) => _session.ClearAsync(cancellationToken);
    public Task SetBrightnessAsync(Lis2Brightness brightness, CancellationToken cancellationToken = default) => _session.SetBrightnessAsync(brightness, cancellationToken);
    public ValueTask DisposeAsync() => _session.DisposeAsync();
}
