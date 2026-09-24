using LIS2.Sources;

namespace LIS2.Winamp;

public sealed class WinampDataSource : IDataSource
{
    private static readonly TimeSpan ConnectedTimeout = TimeSpan.FromSeconds(3);
    private readonly WinampPipeServer _server = new();

    private IReadOnlyDictionary<string, object?> _values =
        CreateInitialValues();

    public string Id => "Winamp";

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
                : Math.Max(0, (DateTimeOffset.UtcNow - LastSnapshotAt.Value).TotalSeconds);

            if (!connected)
                result["State"] = WinampPlaybackState.Unknown.ToString();

            return result;
        }
    }

    public DateTimeOffset? LastSnapshotAt { get; private set; }

    public bool IsRecentlyConnected =>
        LastSnapshotAt is not null &&
        DateTimeOffset.UtcNow - LastSnapshotAt.Value < ConnectedTimeout;

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
            ["SampleRateHz"] = null,
            ["VuLeft"] = null,
            ["VuRight"] = null,
            ["Vu"] = string.Empty,
            ["Spectrum"] = string.Empty,
            ["Connected"] = false,
            ["SnapshotAgeSeconds"] = null
        };

    public async ValueTask DisposeAsync()
    {
        _server.SnapshotReceived -= Server_SnapshotReceived;
        await _server.DisposeAsync().ConfigureAwait(false);
    }
}
