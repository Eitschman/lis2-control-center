namespace LIS2.Sources;

public sealed class ClockDataSource : IDataSource
{
    private IReadOnlyDictionary<string, object?> _values =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public string Id => "Clock";

    public IReadOnlyDictionary<string, object?> Values =>
        Volatile.Read(ref _values);

    public event EventHandler? Changed;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_loopTask is not null)
            return Task.CompletedTask;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopTask = RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is null)
            return;

        await _cts.CancelAsync().ConfigureAwait(false);

        if (_loopTask is not null)
        {
            try
            {
                await _loopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _cts.Dispose();
        _cts = null;
        _loopTask = null;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (!cancellationToken.IsCancellationRequested)
        {
            UpdateValues();

            if (!await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                break;
        }
    }

    private void UpdateValues()
    {
        var now = DateTime.Now;

        IReadOnlyDictionary<string, object?> next =
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Time"] = now.ToString("HH:mm:ss"),
                ["Date"] = now.ToString("dd.MM.yyyy"),
                ["Day"] = now.ToString("dddd")
            };

        Volatile.Write(ref _values, next);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
