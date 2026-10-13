using LIS2.Core;

namespace LIS2.App.Avalonia;

/// <summary>
/// A hardware-free LIS2 session used while porting the shared UI.
/// Real serial and fan control must be added through a separate, safety-reviewed service.
/// </summary>
public sealed class VirtualDeviceSession : IAsyncDisposable
{
    private readonly VirtualLis2Transport _transport = new();
    private readonly Lis2Device _device;

    public VirtualDeviceSession()
    {
        _device = new Lis2Device(_transport);
    }

    public bool IsConnected => _device.IsConnected;
    public int CommandCount => _transport.Writes.Count;
    public VirtualLis2State State => _transport.State;

    public Task WriteLineAsync(int line, string text, CancellationToken cancellationToken = default) =>
        _device.WriteLineAsync(line, text, cancellationToken);

    public async Task RenderBothAsync(string line1, string line2, CancellationToken cancellationToken = default)
    {
        await _device.WriteLineAsync(1, line1, cancellationToken);
        await _device.WriteLineAsync(2, line2, cancellationToken);
    }

    public Task SendSpecialCharacterTestAsync(CancellationToken cancellationToken = default) =>
        RenderBothAsync("ÄÖÜ äöü ß ° µ ± £", "← → Ω π Σ √ ∞ × ÷", cancellationToken);

    public async Task SendCharacterRomRangeAsync(int start, CancellationToken cancellationToken = default)
    {
        if (start is < 0x20 or > 0xD8)
            throw new ArgumentOutOfRangeException(nameof(start), "ROM start byte must be hexadecimal 20 through D8.");

        var firstLine = Enumerable.Range(start, 20).Select(value => (byte)value).ToArray();
        var secondLine = Enumerable.Range(start + 20, 20).Select(value => (byte)value).ToArray();
        await _device.WriteRawDisplayBytesAsync(1, 0, firstLine, cancellationToken);
        await _device.WriteRawDisplayBytesAsync(2, 0, secondLine, cancellationToken);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        _device.ClearAsync(cancellationToken);

    public Task SetBrightnessAsync(Lis2Brightness brightness, CancellationToken cancellationToken = default) =>
        _device.SetBrightnessAsync(brightness, cancellationToken);

    public Task ConnectAsync(CancellationToken cancellationToken = default) =>
        _device.ConnectAsync(cancellationToken);

    public Task DisconnectAsync(CancellationToken cancellationToken = default) =>
        _device.DisconnectAsync(cancellationToken);

    public async Task WriteDemoAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Connect the virtual LIS2 first.");

        await _device.ClearAsync(cancellationToken);
        await _device.WriteLineAsync(1, "LIS2 Control Center", cancellationToken);
        await _device.WriteLineAsync(2, "Avalonia Windows/Linux", cancellationToken);
    }

    public ValueTask DisposeAsync() => _device.DisposeAsync();
}
