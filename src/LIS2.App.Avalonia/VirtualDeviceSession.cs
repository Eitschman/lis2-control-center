using LIS2.Core;
using LIS2.Display;

namespace LIS2.App.Avalonia;

/// <summary>
/// A hardware-free LIS2 session used while porting the shared UI.
/// Real serial and fan control must be added through a separate, safety-reviewed service.
/// </summary>
public sealed class VirtualDeviceSession : IDisplaySession
{
    private readonly VirtualLis2Transport _transport = new();
    private readonly Lis2Device _device;
    private readonly DisplayFrameWriter _frameWriter;
    private readonly SemaphoreSlim _gate = new(1, 1);
    public DiagnosticJournal Journal { get; } = new();

    public VirtualDeviceSession()
    {
        _device = new Lis2Device(new DiagnosticTransport(_transport, Journal));
        _frameWriter = new DisplayFrameWriter(_device);
    }

    public bool IsConnected => _device.IsConnected;
    public int CommandCount => _transport.Writes.Count;
    public IReadOnlyList<byte[]> CommandHistory => _transport.Writes;
    public VirtualLis2State State => _transport.State;

    public Task WriteLineAsync(int line, string text, CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => _device.WriteLineAsync(line, text, cancellationToken), true, cancellationToken);

    public async Task RenderBothAsync(string line1, string line2, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(() => _frameWriter.WriteAsync(DisplayFrame.Create(line1, line2), cancellationToken), false, cancellationToken);
    }

    public Task SendSpecialCharacterTestAsync(CancellationToken cancellationToken = default) =>
        RenderBothAsync("ÄÖÜ äöü ß ° µ ± £", "← → Ω π Σ √ ∞ × ÷", cancellationToken);

    public async Task SendCharacterRomRangeAsync(int start, CancellationToken cancellationToken = default)
    {
        if (start is < 0x20 or > 0xD8)
            throw new ArgumentOutOfRangeException(nameof(start), "ROM start byte must be hexadecimal 20 through D8.");

        var firstLine = Enumerable.Range(start, 20).Select(value => (byte)value).ToArray();
        var secondLine = Enumerable.Range(start + 20, 20).Select(value => (byte)value).ToArray();
        await WriteRawDisplayBytesAsync(1, 0, firstLine, cancellationToken);
        await WriteRawDisplayBytesAsync(2, 0, secondLine, cancellationToken);
    }

    public Task ProgramGlyphAsync(int slot, byte[] rows, CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => _device.ProgramCharacterAsync(slot, rows, cancellationToken), false, cancellationToken);

    public async Task ProgramAllGlyphsAsync(IReadOnlyList<CustomGlyphSettings> glyphs, CancellationToken cancellationToken = default)
    {
        if (glyphs.Count != 8 || glyphs.Any(g => g.Rows is null || g.Rows.Length != 8 || g.Rows.Any(row => row > 31)))
            throw new ArgumentException("Exactly eight valid 5x8 glyphs are required.", nameof(glyphs));

        for (var slot = 1; slot <= 8; slot++)
            await ProgramGlyphAsync(slot, glyphs[slot - 1].Rows, cancellationToken);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => _device.ClearAsync(cancellationToken), true, cancellationToken);

    public Task WriteRawDisplayBytesAsync(int line, int column, byte[] bytes, CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => _device.WriteRawDisplayBytesAsync(line, column, bytes, cancellationToken), true, cancellationToken);

    public Task SetFansAsync(IReadOnlyList<int> outputs) =>
        ExecuteAsync(() => _device.SetFansAsync(outputs[0], outputs[1], outputs[2], outputs[3]), false);

    public Task SetBrightnessAsync(Lis2Brightness brightness, CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => _device.SetBrightnessAsync(brightness, cancellationToken), false, cancellationToken);

    public Task ConnectAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => _device.ConnectAsync(cancellationToken), true, cancellationToken);

    public Task DisconnectAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => _device.DisconnectAsync(cancellationToken), true, cancellationToken);

    public async Task WriteDemoAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Connect the virtual LIS2 first.");

        await ClearAsync(cancellationToken);
        await RenderBothAsync("LIS2 Control Center", "Avalonia Windows/Linux", cancellationToken);
    }

    public async ValueTask DisposeAsync() => await ExecuteAsync(() => _device.DisposeAsync().AsTask(), true);

    /// <summary>Manuelle Ausgabe und Seitenframes teilen einen Schreibbesitzer und invalidieren den Cache.</summary>
    private async Task ExecuteAsync(Func<Task> action, bool reset, CancellationToken token = default)
    {
        await _gate.WaitAsync(token);
        try
        {
            if (reset) _frameWriter.Reset();
            await action();
        }
        finally { _gate.Release(); }
    }
}
