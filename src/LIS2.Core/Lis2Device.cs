namespace LIS2.Core;

public sealed class Lis2Device : ILis2Device
{
    private readonly ILis2Transport _transport;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public Lis2Device(ILis2Transport transport) =>
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));

    public bool IsConnected => _transport.IsOpen;

    public Task ConnectAsync(CancellationToken cancellationToken = default) =>
        _transport.OpenAsync(cancellationToken);

    public Task DisconnectAsync(CancellationToken cancellationToken = default) =>
        _transport.CloseAsync(cancellationToken);

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        SendAsync(Lis2Protocol.Clear, cancellationToken);

    public Task WriteLineAsync(int line, string text, CancellationToken cancellationToken = default) =>
        WriteAsync(line, 0, text, cancellationToken);

    public Task WriteAsync(int line, int column, string text, CancellationToken cancellationToken = default) =>
        SendAsync(Lis2Protocol.WriteLine(line, column, text), cancellationToken);

    public Task SetBrightnessAsync(Lis2Brightness brightness, CancellationToken cancellationToken = default) =>
        SendAsync(Lis2Protocol.SetBrightness(brightness), cancellationToken);

    public Task SetFansAsync(int fan1, int fan2, int fan3, int fan4, CancellationToken cancellationToken = default) =>
        SendAsync(Lis2Protocol.SetFans(fan1, fan2, fan3, fan4), cancellationToken);

    public async Task ProgramCharacterAsync(int slot, ReadOnlyMemory<byte> rows, CancellationToken cancellationToken = default)
    {
        if (rows.Length != 8)
            throw new ArgumentException("A custom LIS2 character must contain exactly eight rows.", nameof(rows));

        for (var row = 0; row < 8; row++)
            await SendAsync(Lis2Protocol.ProgramCharacterRow(slot, row, rows.Span[row]), cancellationToken).ConfigureAwait(false);
    }

    private async Task SendAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken)
    {
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _transport.WriteAsync(command, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _writeGate.Dispose();
        await _transport.DisposeAsync().ConfigureAwait(false);
    }
}
