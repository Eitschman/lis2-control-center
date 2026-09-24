using System.Runtime.ExceptionServices;

namespace LIS2.Display;

/// <summary>
/// Serializes asynchronous work and collapses bursts of requests into the
/// smallest number of executions needed to observe the latest state.
/// </summary>
public sealed class AsyncCoalescingRunner
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private int _requested;

    public async Task RunAsync(
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        Interlocked.Exchange(ref _requested, 1);

        if (!await _gate.WaitAsync(0, cancellationToken))
            return;

        ExceptionDispatchInfo? failure = null;

        try
        {
            while (Interlocked.Exchange(ref _requested, 0) == 1)
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    failure = ExceptionDispatchInfo.Capture(ex);
                    break;
                }
            }
        }
        finally
        {
            _gate.Release();
        }

        // A request may arrive after the last loop check but before the gate
        // is released. That requester observes a busy gate and returns, so the
        // runner that just released the gate must pick it up.
        if (Volatile.Read(ref _requested) == 1)
            await RunAsync(action, cancellationToken);

        failure?.Throw();
    }
}
