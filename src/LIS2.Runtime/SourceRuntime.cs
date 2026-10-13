using LIS2.Sources;

namespace LIS2.Runtime;

/// <summary>
/// Owns application source lifecycle, configuration and source diagnostics.
/// Serializes configuration and start/stop to avoid source races during shutdown.
/// </summary>
public sealed class SourceRuntime : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly DataSourceRegistry _registry = new();
    private readonly HomeAssistantDataSource _homeAssistant = new();
    private bool _started;
    private bool _disposed;

    public SourceRuntime()
    {
        _registry.Add(new ClockDataSource());
        _registry.Add(_homeAssistant);
    }

    public HomeAssistantDataSource HomeAssistant => _homeAssistant;
    public IReadOnlyDictionary<string, string?> Errors => _registry.Errors;
    public IReadOnlyDictionary<string, object?> Snapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _registry.Snapshot();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_started) return;
            await _registry.StartAllAsync(cancellationToken).ConfigureAwait(false);
            _started = true;
        }
        finally { _gate.Release(); }
    }

    public async Task ConfigureHomeAssistantAsync(string? url, string? token,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await _homeAssistant.StopAsync(cancellationToken).ConfigureAwait(false);
            _homeAssistant.Configure(url, token);
            if (_started) await _homeAssistant.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed) return;
            _disposed = true;
            await _registry.StopAllAsync().ConfigureAwait(false);
            await _registry.DisposeAsync().ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }
}
