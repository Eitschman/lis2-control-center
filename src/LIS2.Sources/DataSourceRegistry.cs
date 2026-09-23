namespace LIS2.Sources;

public sealed class DataSourceRegistry : IAsyncDisposable
{
    private readonly Dictionary<string, IDataSource> _sources = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<IDataSource> Sources => _sources.Values;

    public void Add(IDataSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!_sources.TryAdd(source.Id, source))
            throw new InvalidOperationException($"A data source with id '{source.Id}' is already registered.");
    }

    public IReadOnlyDictionary<string, object?> Snapshot()
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in _sources.Values)
        {
            foreach (var pair in source.Values)
                result[$"{source.Id}.{pair.Key}"] = pair.Value;
        }

        return result;
    }

    public async Task StartAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var source in _sources.Values)
            await source.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var source in _sources.Values)
            await source.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var source in _sources.Values)
            await source.DisposeAsync().ConfigureAwait(false);
    }
}
