namespace LIS2.Core;

public interface ILis2Transport : IAsyncDisposable
{
    bool IsOpen { get; }

    Task OpenAsync(CancellationToken cancellationToken = default);
    Task CloseAsync(CancellationToken cancellationToken = default);
    Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
}
