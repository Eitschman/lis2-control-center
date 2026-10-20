using LIS2.Core;

namespace LIS2.App.Avalonia;

/// <summary>Begrenztes Sitzungsprotokoll ohne Zugangsdaten oder Quellensnapshots.</summary>
public sealed class DiagnosticJournal(int capacity = 1000)
{
    private readonly Queue<string> _entries = new();
    private readonly object _sync = new();

    public void Record(string category, string message)
    {
        lock (_sync)
        {
            _entries.Enqueue($"{DateTimeOffset.UtcNow:O} [{category}] {message}");
            while (_entries.Count > Math.Max(1, capacity)) _entries.Dequeue();
        }
    }

    public string Snapshot()
    {
        lock (_sync) return string.Join(Environment.NewLine, _entries);
    }
}

/// <summary>Protokolliert Transportergebnisse. Fehlgeschlagene Schreibversuche sind ausdrücklich markiert.</summary>
internal sealed class DiagnosticTransport(ILis2Transport transport, DiagnosticJournal journal) : ILis2Transport
{
    public bool IsOpen => transport.IsOpen;
    public Task OpenAsync(CancellationToken cancellationToken = default) =>
        RunAsync("Open", () => transport.OpenAsync(cancellationToken));
    public Task CloseAsync(CancellationToken cancellationToken = default) =>
        RunAsync("Close", () => transport.CloseAsync(cancellationToken));
    public Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
        RunAsync("Write " + Convert.ToHexString(data.Span) + " " + Lis2Protocol.DescribeCommand(data.ToArray()),
            () => transport.WriteAsync(data, cancellationToken));
    public async ValueTask DisposeAsync() => await RunAsync("Dispose", () => transport.DisposeAsync().AsTask());

    private async Task RunAsync(string operation, Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
            journal.Record("Transport", "OK " + operation);
        }
        catch (Exception exception)
        {
            journal.Record("Transport", "ERROR " + operation + " (" + exception.GetType().Name + ")");
            throw;
        }
    }
}
