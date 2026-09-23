using LIS2.Sources;

namespace LIS2.Winamp;

public sealed class WinampDataSource : IDataSource
{
    private readonly WinampPipeServer _server = new();
    private IReadOnlyDictionary<string, object?> _values =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["State"] = WinampPlaybackState.Unknown.ToString(),
            ["Artist"] = null,
            ["Title"] = null,
            ["Album"] = null,
            ["PlaylistPosition"] = null,
            ["PlaylistCount"] = null,
            ["Elapsed"] = null,
            ["Duration"] = null,
            ["BitrateKbps"] = null,
            ["SampleRateHz"] = null
        };

    public string Id => "Winamp";

    public IReadOnlyDictionary<string, object?> Values =>
        Volatile.Read(ref _values);

    public event EventHandler? Changed;

    public WinampDataSource()
    {
        _server.SnapshotReceived += Server_SnapshotReceived;
    }

    public Task StartAsync(CancellationToken cancellationToken = default) =>
        _server.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default) =>
        _server.StopAsync();

    private void Server_SnapshotReceived(object? sender, WinampSnapshot snapshot)
    {
        Volatile.Write(ref _values, WinampValues.FromSnapshot(snapshot));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async ValueTask DisposeAsync()
    {
        _server.SnapshotReceived -= Server_SnapshotReceived;
        await _server.DisposeAsync().ConfigureAwait(false);
    }
}
