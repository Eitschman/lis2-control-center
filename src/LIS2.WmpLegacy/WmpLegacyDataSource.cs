using LIS2.Sources;

namespace LIS2.WmpLegacy;

public sealed class WmpLegacyDataSource : IDataSource
{
    private static readonly TimeSpan ConnectedTimeout = TimeSpan.FromSeconds(3);
    private readonly WmpLegacyPipeServer _server = new();
    private readonly TimeProvider _timeProvider;

    private IReadOnlyDictionary<string, object?> _values =
        CreateInitialValues();

    public string Id => "WmpLegacy";

    public IReadOnlyDictionary<string, object?> Values
    {
        get
        {
            var current = Volatile.Read(ref _values);
            var result = new Dictionary<string, object?>(
                current,
                StringComparer.OrdinalIgnoreCase);

            var connected = IsRecentlyConnected;
            result["Connected"] = connected;
            result["SnapshotAgeSeconds"] = LastSnapshotAt is null
                ? null
                : Math.Max(
                    0,
                    (_timeProvider.GetUtcNow() - LastSnapshotAt.Value).TotalSeconds);

            if (!connected)
                result["State"] = WmpLegacyPlaybackState.Unknown.ToString();

            return result;
        }
    }

    public DateTimeOffset? LastSnapshotAt { get; private set; }

    public bool IsRecentlyConnected =>
        LastSnapshotAt is not null &&
        _timeProvider.GetUtcNow() - LastSnapshotAt.Value < ConnectedTimeout;

    public event EventHandler? Changed;

    public WmpLegacyDataSource(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _server.SnapshotReceived += Server_SnapshotReceived;
    }

    public Task StartAsync(CancellationToken cancellationToken = default) =>
        _server.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default) =>
        _server.StopAsync();

    private void Server_SnapshotReceived(object? sender, WmpLegacySnapshot snapshot) =>
        ApplySnapshot(snapshot);

    internal void ApplySnapshot(WmpLegacySnapshot snapshot)
    {
        LastSnapshotAt = _timeProvider.GetUtcNow();
        Volatile.Write(ref _values, WmpLegacyValues.FromSnapshot(snapshot));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static IReadOnlyDictionary<string, object?> CreateInitialValues() =>
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["State"] = WmpLegacyPlaybackState.Unknown.ToString(),
            ["Artist"] = null,
            ["Title"] = null,
            ["Album"] = null,
            ["TrackNumber"] = null,
            ["PlaylistCount"] = null,
            ["Elapsed"] = null,
            ["Duration"] = null,
            ["Connected"] = false,
            ["SnapshotAgeSeconds"] = null
        };

    public async ValueTask DisposeAsync()
    {
        _server.SnapshotReceived -= Server_SnapshotReceived;
        await _server.DisposeAsync().ConfigureAwait(false);
    }
}
