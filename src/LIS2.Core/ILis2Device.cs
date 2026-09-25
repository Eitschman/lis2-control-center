namespace LIS2.Core;

public interface ILis2Device : IAsyncDisposable
{
    bool IsConnected { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
    Task WriteLineAsync(int line, string text, CancellationToken cancellationToken = default);
    Task WriteAsync(int line, int column, string text, CancellationToken cancellationToken = default);
    Task WriteRawDisplayBytesAsync(int line, int column, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default);
    Task SetBrightnessAsync(Lis2Brightness brightness, CancellationToken cancellationToken = default);
    Task SetFansAsync(int fan1, int fan2, int fan3, int fan4, CancellationToken cancellationToken = default);
    Task ProgramCharacterAsync(int slot, ReadOnlyMemory<byte> rows, CancellationToken cancellationToken = default);
}
