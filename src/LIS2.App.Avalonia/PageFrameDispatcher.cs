using LIS2.Display;

namespace LIS2.App.Avalonia;

/// <summary>
/// UI-independent page frame dispatch: serializes writes, skips identical frames,
/// clears stale content when no page is visible, and never issues fan commands.
/// The caller owns scheduling and transport lifecycle.
/// </summary>
public sealed class PageFrameDispatcher
{
    private readonly Func<DisplayFrame, CancellationToken, Task> _write;
    private readonly Func<CancellationToken, Task> _clear;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DisplayFrame? _last;
    private bool _hasFrame;

    public PageFrameDispatcher(
        Func<DisplayFrame, CancellationToken, Task> write,
        Func<CancellationToken, Task> clear)
    {
        _write = write ?? throw new ArgumentNullException(nameof(write));
        _clear = clear ?? throw new ArgumentNullException(nameof(clear));
    }

    public async Task<bool> DispatchAsync(DisplayFrame? frame, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (frame is null)
            {
                if (!_hasFrame) return false;
                await _clear(cancellationToken).ConfigureAwait(false);
                _last = null;
                _hasFrame = false;
                return true;
            }

            if (_hasFrame && frame == _last) return false;
            await _write(frame, cancellationToken).ConfigureAwait(false);
            _last = frame;
            _hasFrame = true;
            return true;
        }
        finally { _gate.Release(); }
    }

    public void Reset()
    {
        // Only call after pending writes have finished (e.g. before playback starts).
        _last = null;
        _hasFrame = false;
    }
}
