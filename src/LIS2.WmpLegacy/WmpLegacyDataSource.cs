using LIS2.Sources;

namespace LIS2.WmpLegacy;

public sealed class WmpLegacyDataSource : IDataSource
{
    private static readonly TimeSpan ConnectedTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan VisualizationTimeout = TimeSpan.FromSeconds(2);

    private readonly WmpLegacyPipeServer _server;
    private readonly WmpLegacyVisualizationPipeServer _visualizationServer;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private CancellationTokenSource? _audioCancellation;
    private Task _audioTask = Task.CompletedTask;
    private bool _started;
    private readonly bool _processAudioEnabled;
    public bool ProcessAudioEnabled => _processAudioEnabled;
    // Metadaten- und Audiopipe dürfen die Änderungen des jeweils anderen Listeners nicht überschreiben.
    private readonly object _snapshotLock = new();

    private IReadOnlyDictionary<string, object?> _values =
        CreateInitialValues();

    public string Id => "WmpLegacy";

    public IReadOnlyDictionary<string, object?> Values
    {
        get
        {
            _server.ThrowIfFaulted();
            _visualizationServer.ThrowIfFaulted();
            lock (_snapshotLock)
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
    }

    private DateTimeOffset? _lastSnapshotAt;
    private DateTimeOffset? _lastVisualizationAt;
    public DateTimeOffset? LastSnapshotAt
    {
        get { lock (_snapshotLock) return _lastSnapshotAt; }
        private set { lock (_snapshotLock) _lastSnapshotAt = value; }
    }
    public DateTimeOffset? LastVisualizationAt
    {
        get { lock (_snapshotLock) return _lastVisualizationAt; }
        private set { lock (_snapshotLock) _lastVisualizationAt = value; }
    }

    public bool IsRecentlyConnected =>
        LastSnapshotAt is not null &&
        _timeProvider.GetUtcNow() - LastSnapshotAt.Value < ConnectedTimeout;

    public bool IsVisualizationRecentlyConnected =>
        LastVisualizationAt is not null &&
        _timeProvider.GetUtcNow() - LastVisualizationAt.Value < VisualizationTimeout;

    public event EventHandler? Changed;

    public WmpLegacyDataSource(TimeProvider? timeProvider = null,
        string metadataPipeName = WmpLegacyPipeServer.PipeName,
        string visualizationPipeName = WmpLegacyVisualizationPipeServer.PipeName,
        bool enableProcessAudio = false)
    {
        _processAudioEnabled = enableProcessAudio;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _server = new(metadataPipeName);
        _visualizationServer = new(visualizationPipeName);
        _server.SnapshotReceived += Server_SnapshotReceived;
        _visualizationServer.VisualizationReceived += VisualizationServer_VisualizationReceived;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_started) return;
            await _server.StartAsync(cancellationToken).ConfigureAwait(false);
            await _visualizationServer.StartAsync(cancellationToken).ConfigureAwait(false);
            _started = true;
            StartProcessAudio();
        }
        finally { _lifecycle.Release(); }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _started = false;
            await StopProcessAudioAsync().ConfigureAwait(false);
            await Task.WhenAll(_server.StopAsync(), _visualizationServer.StopAsync()).ConfigureAwait(false);
        }
        finally { _lifecycle.Release(); }
    }

    private void StartProcessAudio()
    {
        if (!_started || !_processAudioEnabled) return;
        _audioCancellation = new CancellationTokenSource();
        _audioTask = new Audio.ProcessAudioWorker(() => IsRecentlyConnected,
            ApplyVisualization, SetAudioCaptureError).RunAsync(_audioCancellation.Token);
    }

    private async Task StopProcessAudioAsync()
    {
        if (_audioCancellation is null) return;
        _audioCancellation.Cancel();
        try { await _audioTask.ConfigureAwait(false); }
        finally { _audioCancellation.Dispose(); _audioCancellation = null; }
    }

    private void SetAudioCaptureError(string? error)
    {
        lock (_snapshotLock)
        {
            _lastVisualizationAt = null;
            var next = new Dictionary<string, object?>(_values, StringComparer.OrdinalIgnoreCase)
            {
                ["AudioCaptureError"] = error
            };
            Volatile.Write(ref _values, next);
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Server_SnapshotReceived(object? sender, WmpLegacySnapshot snapshot) =>
        ApplySnapshot(snapshot);

    private void VisualizationServer_VisualizationReceived(
        object? sender,
        WmpLegacyVisualizationMessage message) => ApplyVisualizationPipe(message);

    internal void ApplyVisualizationPipe(WmpLegacyVisualizationMessage message)
    {
        // Die gewählte direkte Quelle darf nicht durch Nullwerte des defekten Player-Visualisierungspfads überschrieben werden.
        if (!_processAudioEnabled) ApplyVisualization(message);
    }

    internal void ApplySnapshot(WmpLegacySnapshot snapshot)
    {
        lock (_snapshotLock)
        {
            LastSnapshotAt = _timeProvider.GetUtcNow();

            var next = new Dictionary<string, object?>(
                Volatile.Read(ref _values),
                StringComparer.OrdinalIgnoreCase);

            foreach (var pair in WmpLegacyValues.FromSnapshot(snapshot))
                next[pair.Key] = pair.Value;

            Volatile.Write(ref _values, next);
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal void ApplyVisualization(WmpLegacyVisualizationMessage message)
    {
        lock (_snapshotLock)
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
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static IReadOnlyDictionary<string, object?> CreateInitialValues() =>
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["State"] = WmpLegacyPlaybackState.Unknown.ToString(),
            ["Artist"] = null,
            ["Title"] = null,
            ["Album"] = null,
            ["StreamName"] = null,
            ["StreamUrl"] = null,
            ["IsStream"] = null,
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
        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try { _started = false; await StopProcessAudioAsync().ConfigureAwait(false); }
        finally { _lifecycle.Release(); }
        await Task.WhenAll(_server.DisposeAsync().AsTask(), _visualizationServer.DisposeAsync().AsTask()).ConfigureAwait(false);
    }
}
