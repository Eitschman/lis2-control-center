using LIS2.Display;

namespace LIS2.App.Avalonia;

/// <summary>
/// Headless application-level owner of page sources and virtual/serial page sequences.
/// Transport delegates are supplied by the UI adapter; no Avalonia dependencies.
/// </summary>
public sealed class DisplayRuntimeHost : IAsyncDisposable
{
    private readonly Func<DateTimeOffset> _clock;
    private readonly PageSourceValues _sources;
    private readonly List<PageSequenceController> _sequences = new();
    private readonly EventQueue _events = new();
    private bool _disposed;

    public DisplayRuntimeHost(PageSourceValues? sources = null, Func<DateTimeOffset>? clock = null)
    {
        _sources = sources ?? new PageSourceValues();
        _clock = clock ?? (() => DateTimeOffset.Now);
    }

    public PageSourceValues Sources => _sources;
    public EventQueue Events => _events;
    public void EnqueueEvent(DisplayEvent displayEvent)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _events.Add(displayEvent);
    }

    public Task StartSourcesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _sources.StartAsync(cancellationToken);
    }

    public IReadOnlyDictionary<string, object?> Snapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _sources.Snapshot(_clock());
    }

    public PageSequenceController CreateSequence(
        Func<DisplayFrame, CancellationToken, Task> write,
        Func<CancellationToken, Task> clear)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var sequence = new PageSequenceController(write, clear, _events);
        _sequences.Add(sequence);
        return sequence;
    }

    public Task<bool> TickAsync(PageSequenceController sequence, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(sequence);
        if (!_sequences.Contains(sequence))
            throw new ArgumentException("Sequence is not owned by this runtime.", nameof(sequence));
        return sequence.TickAsync(_clock(), Snapshot(), cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var sequence in _sequences) sequence.Stop();
        foreach (var sequence in _sequences)
            await sequence.StopAndDrainAsync().ConfigureAwait(false);
        await _sources.DisposeAsync().ConfigureAwait(false);
    }
}
