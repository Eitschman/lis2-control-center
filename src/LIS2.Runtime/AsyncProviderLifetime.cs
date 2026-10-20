namespace LIS2.Runtime;

/// <summary>Start und Abbau eines Anbieters überlappen nie, auch wenn der Start verzögert endet.</summary>
public sealed class AsyncProviderLifetime
{
    private readonly object _sync = new();
    private Task _startup = Task.CompletedTask;
    private Task? _shutdown;
    private bool _started;

    public Task StartAsync(Func<Task> start)
    {
        lock (_sync)
        {
            if (_shutdown is not null) throw new InvalidOperationException("Provider is stopping.");
            if (_started) return _startup;
            _started = true;
            return _startup = start();
        }
    }

    public Task StopAsync(Func<Task> cleanup)
    {
        lock (_sync)
            return _shutdown ??= RuntimeShutdown.RunAsync(() => _startup, cleanup);
    }
}
