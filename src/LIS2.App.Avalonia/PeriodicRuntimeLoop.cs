namespace LIS2.App.Avalonia;

/// <summary>
/// Headless cancellation-aware periodic driver. No UI framework or transport dependencies.
/// The callback must handle its own synchronization; overlapping ticks are never scheduled.
/// </summary>
public sealed class PeriodicRuntimeLoop : IAsyncDisposable
{
    private readonly TimeSpan _interval;
    private readonly Func<CancellationToken, Task> _tick;
    private readonly object _sync = new();
    private CancellationTokenSource? _stop;
    private Task? _running;

    public PeriodicRuntimeLoop(TimeSpan interval, Func<CancellationToken, Task> tick)
    {
        if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));
        _tick = tick ?? throw new ArgumentNullException(nameof(tick));
        _interval = interval;
    }

    public bool IsRunning
    {
        get { lock (_sync) return _stop is not null; }
    }

    public bool Start()
    {
        lock (_sync)
        {
            if (_stop is not null || _running is { IsCompleted: false }) return false;
            _stop = new CancellationTokenSource();
            var token = _stop.Token;
            _running = RunAsync(token);
            return true;
        }
    }

    public void Stop()
    {
        lock (_sync) _stop?.Cancel();
    }

    public async Task StopAndDrainAsync()
    {
        Task? running;
        CancellationTokenSource? stop;
        lock (_sync)
        {
            stop = _stop;
            running = _running;
            stop?.Cancel();
        }
        try
        {
            if (running is not null) await running.ConfigureAwait(false);
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_stop, stop))
                {
                    _stop = null;
                    _running = null;
                    stop?.Dispose();
                }
            }
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                await _tick(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch
        {
            Stop();
            throw;
        }
    }

    public async ValueTask DisposeAsync() => await StopAndDrainAsync().ConfigureAwait(false);
}
