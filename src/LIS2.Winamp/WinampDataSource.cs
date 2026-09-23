using LIS2.Sources;

namespace LIS2.Winamp;

public sealed class WinampDataSource : IDataSource
{
    private readonly WinampPipeServer _server = new();

    private IReadOnlyDictionary<string, object?> _values =
        CreateInitialValues();

    public string Id => "Winamp";

    public IReadOnlyDictionary<string, object?> Values =>
        Volatile.Read(ref _values);

    public DateTimeOffset? LastSnapshotAt { get; private set; }

    public bool IsRecentlyConnected =>
        LastSnapshotAt is not null &&
        DateTimeOffset.UtcNow - LastSnapshotAt.Value < TimeSpan.FromSeconds(3);

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
        LastSnapshotAt = DateTimeOffset.UtcNow;

        Volatile.Write(
            ref _values,
            WinampValues.FromSnapshot(snapshot));

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static IReadOnlyDictionary<string, object?> CreateInitialValues() =>
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

    public async ValueTask DisposeAsync()
    {
        _server.SnapshotReceived -= Server_SnapshotReceived;
        await _server.DisposeAsync().ConfigureAwait(false);
    }
}
