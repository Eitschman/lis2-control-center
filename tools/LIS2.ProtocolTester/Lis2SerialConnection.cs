using System.IO.Ports;

namespace LIS2.ProtocolTester;

internal sealed class Lis2SerialConnection : IDisposable
{
    private SerialPort? _port;

    public bool IsOpen => _port?.IsOpen == true;

    public void Open(string portName)
    {
        if (IsOpen)
            return;

        var port = new SerialPort(portName, 19200, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            DtrEnable = false,
            RtsEnable = false,
            ReadTimeout = 500,
            WriteTimeout = 1000
        };

        port.Open();
        _port = port;
    }

    public void Close()
    {
        if (_port is null)
            return;

        if (_port.IsOpen)
            _port.Close();

        _port.Dispose();
        _port = null;
    }

    public void Send(ReadOnlySpan<byte> command)
    {
        if (_port?.IsOpen != true)
            throw new InvalidOperationException("LIS2 serial port is not connected.");

        var data = command.ToArray();
        _port.Write(data, 0, data.Length);
    }

    public void Dispose() => Close();
}
