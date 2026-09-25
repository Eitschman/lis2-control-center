using LIS2.Sources;

namespace LIS2.WmpLegacy;

public sealed class WmpLegacyDataSource : IDataSource
{
    private static readonly TimeSpan ConnectedTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan VisualizationTimeout = TimeSpan.FromSeconds(2);

    private readonly WmpLegacyPipeServer _server = new();
    private readonly WmpLegacyVisualizationPipeServer _visualizationServer = new();
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
            var visualizationConnected = IsVisualizationRecentlyConnected;

            result["Connected"] = connected;
            result["SnapshotAgeSeconds"] = LastSnapshotAt is null
                ? null
                : Math.Max(
                    0,
                    (_timeProvider.GetUtcNow() - LastSnapshotAt.Value).TotalSeconds);

            result["VisualizationConnected"] = visualizationConnected;
            result["VisualizationAgeSeconds"] = LastVisualizationAt is null
                ? null
                : Math.Max(
                    0,
                    (_timeProvider.GetUtcNow() - LastVisualizationAt.Value).TotalSeconds);

            if (!connected)
                result["State"] = WmpLegacyPlaybackState.Unknown.ToString();

            if (!visualizationConnected)
            {
                result["VuLeft"] = null;
                result["VuRight"] = null;
                result["Vu"] = string.Empty;
                result["Spectrum"] = string.Empty;
                result["SpectrumRaw"] = null;
                result["SpectrumPeak"] = null;
            }

            return result;
        }
    }

    public DateTimeOffset? LastSnapshotAt { get; private set; }
    public DateTimeOffset? LastVisualizationAt { get; private set; }

    public bool IsRecentlyConnected =>
        LastSnapshotAt is not null &&
        _timeProvider.GetUtcNow() - LastSnapshotAt.Value < ConnectedTimeout;

    public bool IsVisualizationRecentlyConnected =>
        LastVisualizationAt is not null &&
        _timeProvider.GetUtcNow() - LastVisualizationAt.Value < VisualizationTimeout;

    public event EventHandler? Changed;

    public WmpLegacyDataSource(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _server.SnapshotReceived += Server_SnapshotReceived;
        _visualizationServer.VisualizationReceived += VisualizationServer_VisualizationReceived;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _server.StartAsync(cancellationToken).ConfigureAwait(false);
        await _visualizationServer.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _server.StopAsync().ConfigureAwait(false);
        await _visualizationServer.StopAsync().ConfigureAwait(false);
    }

    private void Server_SnapshotReceived(object? sender, WmpLegacySnapshot snapshot) =>
        ApplySnapshot(snapshot);

    private void VisualizationServer_VisualizationReceived(
        object? sender,
        WmpLegacyVisualizationMessage message) =>
        ApplyVisualization(message);

    internal void ApplySnapshot(WmpLegacySnapshot snapshot)
    {
        LastSnapshotAt = _timeProvider.GetUtcNow();

        var next = new Dictionary<string, object?>(
            Volatile.Read(ref _values),
            StringComparer.OrdinalIgnoreCase);

        foreach (var pair in WmpLegacyValues.FromSnapshot(snapshot))
            next[pair.Key] = pair.Value;

        Volatile.Write(ref _values, next);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal void ApplyVisualization(WmpLegacyVisualizationMessage message)
    {
        LastVisualizationAt = _timeProvider.GetUtcNow();

        var spectrum = message.Spectrum?
            .Select(value => Math.Clamp(value, 0, 255))
            .ToArray();

        var next = new Dictionary<string, object?>(
            Volatile.Read(ref _values),
            StringComparer.OrdinalIgnoreCase)
        {
            ["VuLeft"] = message.VuLeft,
            ["VuRight"] = message.VuRight,
            ["Vu"] = WmpLegacyValues.FormatVu(message.VuLeft, message.VuRight),
            ["Spectrum"] = WmpLegacyValues.FormatSpectrum(spectrum),
            ["SpectrumRaw"] = spectrum,
            ["SpectrumPeak"] = spectrum is { Length: > 0 }
                ? spectrum.Max()
                : null
        };

        Volatile.Write(ref _values, next);
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
            ["VuLeft"] = null,
            ["VuRight"] = null,
            ["Vu"] = string.Empty,
            ["Spectrum"] = string.Empty,
            ["SpectrumRaw"] = null,
            ["SpectrumPeak"] = null,
            ["Connected"] = false,
            ["SnapshotAgeSeconds"] = null,
            ["VisualizationConnected"] = false,
            ["VisualizationAgeSeconds"] = null
        };

    public async ValueTask DisposeAsync()
    {
        _server.SnapshotReceived -= Server_SnapshotReceived;
        _visualizationServer.VisualizationReceived -= VisualizationServer_VisualizationReceived;
        await _server.DisposeAsync().ConfigureAwait(false);
        await _visualizationServer.DisposeAsync().ConfigureAwait(false);
    }
}
