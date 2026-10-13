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
        await _device.WriteLineAsync(0, line1, cancellationToken);
        await _device.WriteLineAsync(1, line2, cancellationToken);
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
        await _device.WriteLineAsync(0, "LIS2 Control Center", cancellationToken);
        await _device.WriteLineAsync(1, "Avalonia Windows/Linux", cancellationToken);
    }

    public ValueTask DisposeAsync() => _device.DisposeAsync();
}
