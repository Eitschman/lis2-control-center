using LIS2.Display;

namespace LIS2.App.Avalonia;

/// <summary>
/// UI-independent lifecycle and output orchestration for one display page sequence.
/// Scheduling is supplied by the host; no Avalonia dispatcher or hardware fan commands.
/// </summary>
public sealed class PageSequenceController
{
    private readonly PagePlayback _playback = new();
    private readonly Func<DisplayFrame, CancellationToken, Task> _write;
    private readonly Func<CancellationToken, Task> _clear;
    private PageFrameDispatcher? _dispatcher;
    private readonly SemaphoreSlim _tickGate = new(1, 1);

    public PageSequenceController(
        Func<DisplayFrame, CancellationToken, Task> write,
        Func<CancellationToken, Task> clear)
    {
        _write = write ?? throw new ArgumentNullException(nameof(write));
        _clear = clear ?? throw new ArgumentNullException(nameof(clear));
    }

    public bool IsRunning { get; private set; }
    public bool IsBusy { get; private set; }
    public string? CurrentPageId => _playback.CurrentPageId;

    public bool Start(IEnumerable<PageDefinition> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);
        if (IsRunning || IsBusy) return false;
        _playback.Reload(pages);
        _dispatcher = new PageFrameDispatcher(_write, _clear);
        IsRunning = true;
        return true;
    }

    public void Stop() => IsRunning = false;

    public async Task<bool> TickAsync(
        DateTimeOffset now,
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (!IsRunning || !await _tickGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            return false;
        IsBusy = true;
        try
        {
            if (!IsRunning) return false;
            var frame = _playback.Tick(now, values);
            return await (_dispatcher ?? throw new InvalidOperationException("Sequence not started."))
                .DispatchAsync(frame, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            Stop();
            throw;
        }
        finally
        {
            IsBusy = false;
            _tickGate.Release();
        }
    }
}
