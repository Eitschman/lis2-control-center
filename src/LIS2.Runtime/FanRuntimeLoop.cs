namespace LIS2.Runtime;

/// <summary>Single-flight periodic fan evaluation with explicit start and drain.</summary>
public sealed class FanRuntimeLoop : IAsyncDisposable
{
    private readonly FanRuntime _runtime;
    private readonly TimeSpan _interval;
    private readonly object _gate = new();
    private CancellationTokenSource? _stop;
    private Task? _task;

    public FanRuntimeLoop(FanRuntime runtime, TimeSpan? interval = null)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _interval = interval ?? TimeSpan.FromSeconds(2);
        if (_interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));
    }

    public bool Start()
    {
        lock (_gate)
        {
            if (_stop is not null) return false;
            _stop = new CancellationTokenSource();
            _task = RunAsync(_stop.Token);
            return true;
        }
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            using var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
                await _runtime.TickAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    public async Task StopAndDrainAsync()
    {
        Task? task;
        CancellationTokenSource? stop;
        lock (_gate)
        {
            stop = _stop;
            task = _task;
            stop?.Cancel();
        }
        try { if (task is not null) await task.ConfigureAwait(false); }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_stop, stop))
                {
                    _stop = null;
                    _task = null;
                    stop?.Dispose();
                }
            }
        }
    }

    public async ValueTask DisposeAsync() => await StopAndDrainAsync().ConfigureAwait(false);
}
