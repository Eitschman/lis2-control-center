namespace LIS2.Sources;

public interface IDataSource : IAsyncDisposable
{
    string Id { get; }

    IReadOnlyDictionary<string, object?> Values { get; }

    event EventHandler? Changed;

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
