using LIS2.Display;

namespace LIS2.App.Avalonia;

/// <summary>
/// UI-independent lifecycle and output orchestration for one display page sequence.
/// Scheduling is supplied by the host; no Avalonia dispatcher or hardware fan commands.
/// </summary>
public sealed class PageSequenceController
{
    private readonly PagePlayback _playback = new();
    private readonly EventQueue _events;
    private readonly Func<DisplayFrame, CancellationToken, Task> _write;
    private readonly Func<CancellationToken, Task> _clear;
    private PageFrameDispatcher? _dispatcher;
    private readonly SemaphoreSlim _tickGate = new(1, 1);

    public PageSequenceController(
        Func<DisplayFrame, CancellationToken, Task> write,
        Func<CancellationToken, Task> clear,
        EventQueue? events = null)
    {
        _write = write ?? throw new ArgumentNullException(nameof(write));
        _clear = clear ?? throw new ArgumentNullException(nameof(clear));
        _events = events ?? new EventQueue();
    }

    public bool IsRunning { get; private set; }
    public bool IsBusy { get; private set; }
    public string? CurrentPageId => _playback.CurrentPageId;
    public TimeSpan NextDelay { get; private set; } = TimeSpan.FromMilliseconds(250);
    public EventQueue Events => _events;

    public bool Start(IEnumerable<PageDefinition> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);
        if (IsRunning || IsBusy) return false;
        _playback.Reload(pages);
        _dispatcher = new PageFrameDispatcher(_write, _clear);
        NextDelay = TimeSpan.FromMilliseconds(1);
        IsRunning = true;
        return true;
    }

    public void Stop() => IsRunning = false;

    /// <summary>Replace the running page schedule without dropping the device connection.
    /// An active display write must finish before the new schedule is installed.</summary>
    public async Task<bool> ReloadAsync(IEnumerable<PageDefinition> pages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pages);
        if (!IsRunning) return false;
        var snapshot = pages.ToArray();
        await _tickGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!IsRunning) return false;
            _playback.Reload(snapshot);
            _dispatcher = new PageFrameDispatcher(_write, _clear);
            NextDelay = TimeSpan.FromMilliseconds(1);
            return true;
        }
        finally { _tickGate.Release(); }
    }

    /// <summary>Stop new ticks and wait for any in-flight display write to finish.</summary>
    public async Task StopAndDrainAsync(CancellationToken cancellationToken = default)
    {
        Stop();
        await _tickGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        _tickGate.Release();
    }

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
            var activeEvent = _events.Peek(now);
            DisplayFrame? frame;
            if (activeEvent is not null)
            {
                frame = activeEvent.Frame;
                var remaining = activeEvent.ExpiresAt - now;
                NextDelay = remaining < TimeSpan.FromMilliseconds(1)
                    ? TimeSpan.FromMilliseconds(1)
                    : remaining < TimeSpan.FromSeconds(1) ? remaining : TimeSpan.FromSeconds(1);
            }
            else
            {
                frame = _playback.Tick(now, values);
                NextDelay = _playback.NextDelay;
            }
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
