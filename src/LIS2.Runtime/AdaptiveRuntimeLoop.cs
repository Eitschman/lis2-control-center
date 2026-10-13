namespace LIS2.App.Avalonia;

/// <summary>
/// UI-independent single-flight display driver. Each tick supplies its next delay,
/// allowing page refresh, rotation and scroll deadlines to drive hardware writes.
/// StopAndDrainAsync awaits an in-flight callback before transport disposal.
/// </summary>
public sealed class AdaptiveRuntimeLoop : IAsyncDisposable
{
    private readonly Func<CancellationToken, Task<TimeSpan>> _tick;
    private readonly object _gate = new();
    private CancellationTokenSource? _stop;
    private Task? _running;

    public AdaptiveRuntimeLoop(Func<CancellationToken, Task<TimeSpan>> tick) =>
        _tick = tick ?? throw new ArgumentNullException(nameof(tick));

    public bool IsRunning { get { lock (_gate) return _stop is not null; } }

    public bool Start()
    {
        lock (_gate)
        {
            if (_stop is not null || _running is { IsCompleted: false }) return false;
            _stop = new CancellationTokenSource();
            _running = RunAsync(_stop.Token);
            return true;
        }
    }

    public void Stop() { lock (_gate) _stop?.Cancel(); }

    public async Task StopAndDrainAsync()
    {
        Task? running;
        CancellationTokenSource? stop;
        lock (_gate) { stop = _stop; running = _running; stop?.Cancel(); }
        try { if (running is not null) await running.ConfigureAwait(false); }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(stop, _stop))
                {
                    _stop = null;
                    _running = null;
                    stop?.Dispose();
                }
            }
        }
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            var delay = TimeSpan.FromMilliseconds(1);
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(delay, token).ConfigureAwait(false);
                var requested = await _tick(token).ConfigureAwait(false);
                delay = requested < TimeSpan.FromMilliseconds(1)
                    ? TimeSpan.FromMilliseconds(1)
                    : requested > TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : requested;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch { Stop(); throw; }
    }

    public async ValueTask DisposeAsync() => await StopAndDrainAsync().ConfigureAwait(false);
}
