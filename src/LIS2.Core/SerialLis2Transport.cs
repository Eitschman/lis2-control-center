using System.IO.Ports;

namespace LIS2.Core;

public sealed class SerialLis2Transport : ILis2Transport
{
    private readonly string _portName;
    private SerialPort? _port;

    public SerialLis2Transport(string portName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);
        _portName = portName;
    }

    public bool IsOpen => _port?.IsOpen == true;

    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (IsOpen)
            return Task.CompletedTask;

        var port = new SerialPort(_portName, 19200, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            DtrEnable = false,
            RtsEnable = false,
            ReadTimeout = 500,
            WriteTimeout = 1000
        };

        port.Open();
        _port = port;
        return Task.CompletedTask;
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_port is null)
            return Task.CompletedTask;

        if (_port.IsOpen)
            _port.Close();

        _port.Dispose();
        _port = null;
        return Task.CompletedTask;
    }

    public Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_port?.IsOpen != true)
            throw new InvalidOperationException("LIS2 serial port is not connected.");

        var buffer = data.ToArray();
        _port.Write(buffer, 0, buffer.Length);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync().ConfigureAwait(false);
    }
}
