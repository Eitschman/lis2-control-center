namespace LIS2.Sources;

public sealed class DataSourceRegistry : IAsyncDisposable
{
    private readonly Dictionary<string, IDataSource> _sources =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string?> _errors =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<IDataSource> Sources => _sources.Values;

    public IReadOnlyDictionary<string, string?> Errors => _errors;

    public void Add(IDataSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!_sources.TryAdd(source.Id, source))
            throw new InvalidOperationException(
                $"A data source with id '{source.Id}' is already registered.");

        _errors[source.Id] = null;
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
        {
            try
            {
                await source.StartAsync(cancellationToken).ConfigureAwait(false);
                _errors[source.Id] = null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _errors[source.Id] = ex.Message;
            }
        }
    }

    public async Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var source in _sources.Values)
        {
            try
            {
                await source.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _errors[source.Id] = ex.Message;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var source in _sources.Values)
        {
            try
            {
                await source.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // Disposal of one optional source must not prevent cleanup of others.
            }
        }
    }
}
