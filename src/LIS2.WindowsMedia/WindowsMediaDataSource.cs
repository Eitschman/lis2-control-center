using LIS2.Sources;
using Windows.Media.Control;
using Windows.Media.Playback;

namespace LIS2.WindowsMedia;

public sealed class WindowsMediaDataSource : IDataSource
{
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private IReadOnlyDictionary<string, object?> _values = CreateInitialValues();
    private bool _started;

    public WindowsMediaDataSource(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public string Id => "WindowsMedia";

    public IReadOnlyDictionary<string, object?> Values
    {
        get
        {
            var current = Volatile.Read(ref _values);
            var result = new Dictionary<string, object?>(
                current,
                StringComparer.OrdinalIgnoreCase);

            result["Connected"] = _session is not null;
            result["SnapshotAgeSeconds"] = LastSnapshotAt is null
                ? null
                : Math.Max(
                    0,
                    (_timeProvider.GetUtcNow() - LastSnapshotAt.Value).TotalSeconds);

            return result;
        }
    }

    public DateTimeOffset? LastSnapshotAt { get; private set; }

    public event EventHandler? Changed;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started)
            return;

        _manager =
            await GlobalSystemMediaTransportControlsSessionManager
                .RequestAsync();

        _manager.SessionsChanged += Manager_SessionsChanged;
        _started = true;

        await SelectSessionAndRefreshAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_manager is not null)
            _manager.SessionsChanged -= Manager_SessionsChanged;

        DetachSession();
        _manager = null;
        _started = false;
        Volatile.Write(ref _values, CreateInitialValues());
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private async void Manager_SessionsChanged(
        GlobalSystemMediaTransportControlsSessionManager sender,
        SessionsChangedEventArgs args)
    {
        try
        {
            await SelectSessionAndRefreshAsync(CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch
        {
            // Optional media telemetry must never destabilize the host.
        }
    }

    private async void Session_MediaPropertiesChanged(
        GlobalSystemMediaTransportControlsSession sender,
        MediaPropertiesChangedEventArgs args) =>
        await RefreshAsyncSafe().ConfigureAwait(false);

    private async void Session_PlaybackInfoChanged(
        GlobalSystemMediaTransportControlsSession sender,
        PlaybackInfoChangedEventArgs args) =>
        await RefreshAsyncSafe().ConfigureAwait(false);

    private async void Session_TimelinePropertiesChanged(
        GlobalSystemMediaTransportControlsSession sender,
        TimelinePropertiesChangedEventArgs args) =>
        await RefreshAsyncSafe().ConfigureAwait(false);

    private async Task RefreshAsyncSafe()
    {
        try
        {
            await RefreshAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Session can disappear between the event and the read.
        }
    }

    private async Task SelectSessionAndRefreshAsync(CancellationToken cancellationToken)
    {
        var manager = _manager;
        if (manager is null)
            return;

        var selected = manager
            .GetSessions()
            .FirstOrDefault(IsWindowsMediaPlayerSession);

        if (!ReferenceEquals(selected, _session))
        {
            DetachSession();
            _session = selected;
            AttachSession();
        }

        if (_session is null)
        {
            Volatile.Write(ref _values, CreateInitialValues());
            Changed?.Invoke(this, EventArgs.Empty);
            return;
        }

        await RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool IsWindowsMediaPlayerSession(
        GlobalSystemMediaTransportControlsSession session)
    {
        var id = session.SourceAppUserModelId ?? string.Empty;

        return id.Contains(
            "Microsoft.ZuneMusic",
            StringComparison.OrdinalIgnoreCase);
    }

    private void AttachSession()
    {
        if (_session is null)
            return;

        _session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
        _session.PlaybackInfoChanged += Session_PlaybackInfoChanged;
        _session.TimelinePropertiesChanged += Session_TimelinePropertiesChanged;
    }

    private void DetachSession()
    {
        if (_session is null)
            return;

        _session.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
        _session.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
        _session.TimelinePropertiesChanged -= Session_TimelinePropertiesChanged;
        _session = null;
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var session = _session;
        if (session is null)
            return;

        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var media = await session
                .TryGetMediaPropertiesAsync();

            var playback = session.GetPlaybackInfo();
            var timeline = session.GetTimelineProperties();

            var state = playback.PlaybackStatus switch
            {
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed =>
                    WindowsMediaPlaybackState.Closed,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Opened =>
                    WindowsMediaPlaybackState.Opened,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Changing =>
                    WindowsMediaPlaybackState.Changing,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped =>
                    WindowsMediaPlaybackState.Stopped,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing =>
                    WindowsMediaPlaybackState.Playing,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused =>
                    WindowsMediaPlaybackState.Paused,
                _ => WindowsMediaPlaybackState.Unknown
            };

            var duration =
                timeline.EndTime > timeline.StartTime
                    ? timeline.EndTime - timeline.StartTime
                    : (TimeSpan?)null;

            var snapshot = new WindowsMediaSnapshot(
                session.SourceAppUserModelId ?? string.Empty,
                state,
                NullIfBlank(media.Artist),
                NullIfBlank(media.Title),
                NullIfBlank(media.AlbumTitle),
                NullIfBlank(media.AlbumArtist),
                media.TrackNumber == 0 ? null : (int?)media.TrackNumber,
                media.AlbumTrackCount == 0 ? null : (int?)media.AlbumTrackCount,
                timeline.Position,
                duration,
                playback.PlaybackRate);

            LastSnapshotAt = _timeProvider.GetUtcNow();
            Volatile.Write(ref _values, WindowsMediaValues.FromSnapshot(snapshot));
            Changed?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyDictionary<string, object?> CreateInitialValues() =>
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["SourceAppUserModelId"] = null,
            ["State"] = WindowsMediaPlaybackState.Unknown.ToString(),
            ["Artist"] = null,
            ["Title"] = null,
            ["Album"] = null,
            ["AlbumArtist"] = null,
            ["TrackNumber"] = null,
            ["AlbumTrackCount"] = null,
            ["Elapsed"] = null,
            ["Duration"] = null,
            ["PlaybackRate"] = null,
            ["Connected"] = false,
            ["SnapshotAgeSeconds"] = null
        };

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _refreshGate.Dispose();
    }
}
