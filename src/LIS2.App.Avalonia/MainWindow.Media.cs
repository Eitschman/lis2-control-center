using Avalonia.Controls;
using Avalonia.Threading;
using LIS2.Winamp;
using LIS2.Runtime;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly WinampDataSource _mediaWinamp = new();
    private DispatcherTimer? _mediaTimer;
    private readonly LinuxMprisSource _mpris = new();
    private readonly WindowsMediaBridge _windowsMedia = new();
    private bool _mprisRefreshing;
    private bool _mediaStopping;
    private readonly AsyncProviderLifetime _mediaLifetime = new();
    private Task _mediaRefresh = Task.CompletedTask;
    private readonly CancellationTokenSource _mediaCancellation = new();
    private readonly Dictionary<string, string> _mediaErrors = new();
    private string? _selectedMediaPlayer;
    private bool _updatingMediaPlayers;

    private void InitializeMedia()
    {
        this.FindControl<Border>("LinuxMediaCard")!.IsVisible = OperatingSystem.IsLinux();
        this.FindControl<Border>("WinampMediaCard")!.IsVisible = OperatingSystem.IsWindows();
        this.FindControl<Border>("WindowsMediaCard")!.IsVisible = OperatingSystem.IsWindows();
        _mediaTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _mediaTimer.Tick += (_, _) => RefreshMedia();
        if (_startBackgroundSources) _mediaTimer.Start();
        this.FindControl<Button>("WindowsMediaPage")!.Click += async (_, _) => await CreateMediaPageAsync("WindowsMedia", "Windows Media Player");
        this.FindControl<Button>("WinampMediaPage")!.Click += async (_, _) =>
            await CreateMediaPageAsync("Winamp", "Winamp");
        this.FindControl<Button>("LinuxMediaPage")!.Click += async (_, _) => await CreateMediaPageAsync("Media", "Linux media");
        this.FindControl<ComboBox>("LinuxMediaPlayer")!.SelectionChanged += async (_, _) =>
        {
            if (_updatingMediaPlayers) return;
            var picker = this.FindControl<ComboBox>("LinuxMediaPlayer")!;
            _selectedMediaPlayer = picker.SelectedIndex <= 0 ? null : picker.SelectedItem as string;
            // Laufende Antworten der vorherigen Auswahl dürfen keine neuen Werte überschreiben.
            _pageSources.UpdateMedia("Media", new Dictionary<string, object?>());
            ApplyMprisValues(LinuxMprisSource.Map("", "", ""));
            await SavePortableWorkspaceAsync();
        };
        if (_startBackgroundSources) _ = _mediaLifetime.StartAsync(StartMediaAsync);
    }

    private async Task StartMediaAsync()
    {
        // Winamp verwendet Windows-Pipes; die zurückgestellte Legacy-Quelle wird nicht gestartet.
        if (!OperatingSystem.IsWindows())
        {
            RefreshMedia();
            return;
        }
        await StartMediaProviderSafeAsync("WindowsMedia", () => _windowsMedia.StartAsync());
        if (_mediaStopping) return;
        this.FindControl<Border>("WindowsMediaCard")!.IsVisible = true;
        await StartMediaProviderSafeAsync("Winamp", () => _mediaWinamp.StartAsync());
        if (!_mediaStopping) RefreshMedia();
    }

    internal async Task StartMediaProviderSafeAsync(string provider, Func<Task> start)
    {
        if (_mediaStopping) return;
        try { await start(); }
        catch (Exception ex)
        {
            _mediaErrors[provider] = ex.GetBaseException().Message;
            _applicationJournal.Record("Media", provider + " startup failed (" + ex.GetBaseException().GetType().Name + ")");
            System.Diagnostics.Trace.TraceError("Media startup {0} failed: {1}: {2}", provider, ex.GetBaseException().GetType().Name, ex.GetBaseException().Message);
            if (_mediaStopping) return;
            _pageSources.UpdateMedia(provider, new Dictionary<string, object?>());
            var controlName = provider switch
            {
                "WindowsMedia" => "WindowsMediaStatus",
                "Winamp" => "WinampMediaStatus",
                "Media" => "LinuxMediaStatus",
                _ => throw new ArgumentException("Unknown media provider.", nameof(provider))
            };
            var status = this.FindControl<TextBlock>(controlName);
            if (status is not null)
            {
                SetUiText(status, $"Provider startup error: {MediaErrorMessage(provider, ex)}");
                status.TextWrapping = global::Avalonia.Media.TextWrapping.Wrap;
                status.MaxWidth = 280;
                status.Foreground = global::Avalonia.Media.Brushes.DarkRed;
                if (status.Parent is Border badge)
                    badge.Background = global::Avalonia.Media.Brush.Parse("#FDE8E8");
            }
        }
    }

    private void RefreshMedia()
    {
        if (_mediaStopping) return;
        if (!OperatingSystem.IsWindows())
        {
            if (_mediaRefresh.IsCompleted) _mediaRefresh = RefreshMprisAsync();
            return;
        }
        RefreshMediaProvider("WindowsMedia", RefreshWindowsMedia);
        RefreshMediaProvider("Winamp", () => RefreshMediaSource("Winamp", _mediaWinamp.Values));
    }

    private void RefreshMediaProvider(string provider, Action refresh)
    {
        if (_mediaErrors.ContainsKey(provider)) return;
        try
        {
            refresh();
        }
        catch (Exception ex)
        {
            _applicationJournal.Record("Media", provider + " refresh failed (" + ex.GetBaseException().GetType().Name + ")");
            var message = ex.GetBaseException().Message;
            System.Diagnostics.Trace.TraceError("Media provider {0} failed: {1}: {2}", provider, ex.GetBaseException().GetType().Name, message);
            var status = provider switch
            {
                "WindowsMedia" => "WindowsMediaStatus",
                "Winamp" => "WinampMediaStatus",
                _ => throw new ArgumentException("Unknown media provider.", nameof(provider))
            };
            _pageSources.UpdateMedia(provider, new Dictionary<string, object?>());
            var statusControl = this.FindControl<TextBlock>(status);
            if (statusControl is not null)
                SetUiText(statusControl, $"Provider error: {message}");
        }
    }

    private void RefreshWindowsMedia()
    {
        var values = _windowsMedia.Values;
        _pageSources.UpdateMedia("WindowsMedia", values);
        string Read(string key) => values.TryGetValue(key, out var value) ? Convert.ToString(value) ?? "" : "";
        SetUiText("WindowsMediaStatus", Read("Connected") == "True" ? "Connected" : "Disconnected");
        this.FindControl<TextBlock>("WindowsMediaTitle")!.Text = Read("Title");
        this.FindControl<TextBlock>("WindowsMediaArtist")!.Text = Read("Artist");
        this.FindControl<TextBlock>("WindowsMediaAlbum")!.Text = Read("Album");
        SetUiText("WindowsMediaPlayback", Read("State"));
        this.FindControl<TextBlock>("WindowsMediaTime")!.Text = Read("Elapsed") + " / " + Read("Duration");
    }

    private async Task RefreshMprisAsync()
    {
        if (_mprisRefreshing) return;
        _mprisRefreshing = true;
        var selectedPlayer = _selectedMediaPlayer;
        try
        {
            var snapshot = await _mpris.ReadSnapshotAsync(selectedPlayer, _mediaCancellation.Token);
            if (_mediaStopping || selectedPlayer != _selectedMediaPlayer) return;
            ApplyMprisSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            if (_mediaStopping || selectedPlayer != _selectedMediaPlayer) return;
            _applicationJournal.Record("Media", "MPRIS refresh failed (" + ex.GetBaseException().GetType().Name + ")");
            System.Diagnostics.Trace.TraceError("Linux MPRIS refresh failed: {0}", ex);
            _pageSources.UpdateMedia("Media", new Dictionary<string, object?>());
            ApplyMprisValues(LinuxMprisSource.Map("", "", ""));
            var status = this.FindControl<TextBlock>("LinuxMediaStatus");
            if (status is not null)
            {
                SetUiText(status, $"Provider error: {MediaErrorMessage("Media", ex)}");
                status.Foreground = global::Avalonia.Media.Brushes.DarkRed;
            }
        }
        finally { _mprisRefreshing = false; }
    }

    internal void UpdateMediaPlayerPicker(IReadOnlyList<string> players)
    {
        _updatingMediaPlayers = true;
        try
        {
            var picker = this.FindControl<ComboBox>("LinuxMediaPlayer")!;
            var choices = new List<string> { UiText("Automatic player") };
            choices.AddRange(players);
            if (_selectedMediaPlayer is not null && !choices.Skip(1).Contains(_selectedMediaPlayer)) choices.Add(_selectedMediaPlayer);
            picker.ItemsSource = choices;
            picker.SelectedIndex = _selectedMediaPlayer is null ? 0 : choices.FindIndex(1, item => item == _selectedMediaPlayer);
        }
        finally { _updatingMediaPlayers = false; }
    }

    internal void ApplyMprisSnapshot(MprisSnapshot snapshot)
    {
        UpdateMediaPlayerPicker(snapshot.Players);
        _pageSources.UpdateMedia("Media", snapshot.Values);
        ApplyMprisValues(snapshot.Values);
    }

    internal void ApplyMprisValues(IReadOnlyDictionary<string, object?> values)
    {
        this.FindControl<TextBlock>("LinuxMediaStatus")!.ClearValue(TextBlock.ForegroundProperty);
        string Read(string key) => values.TryGetValue(key, out var value) ? Convert.ToString(value) ?? "" : "";
        SetUiText("LinuxMediaStatus", Read("Connected") == "True" ? "Connected" : "Disconnected");
        SetUiText("LinuxMediaTitle", string.IsNullOrWhiteSpace(Read("Title")) ? "Nothing playing" : (UiMessage)$"{Read("Title")}");
        foreach (var field in new[] { "Artist", "Album", "Playback", "Duration" })
        {
            var text = field switch
            {
                "Playback" => Read("State"),
                "Duration" => string.IsNullOrEmpty(Read("Elapsed")) && string.IsNullOrEmpty(Read("Duration"))
                    ? "" : Read("Elapsed") + " / " + Read("Duration"),
                _ => Read(field)
            };
            var control = this.FindControl<TextBlock>("LinuxMedia" + field)!;
            control.IsVisible = !string.IsNullOrWhiteSpace(text);
            if (field == "Playback") SetUiText(control, text);
            else control.Text = text;
        }
    }

    private void RefreshMediaSource(string id, IReadOnlyDictionary<string, object?> values)
    {
        _pageSources.UpdateMedia(id, values);
        var prefix = "WinampMedia";
        string Read(string key) => values.TryGetValue(key, out var value) ? Convert.ToString(value) ?? "" : "";
        SetUiText(prefix + "Status", Read("Connected") == "True" ? "Connected" : "Disconnected");
        SetUiText(prefix + "Title", string.IsNullOrWhiteSpace(Read("Title")) ? "Nothing playing" : (UiMessage)$"{Read("Title")}");
        this.FindControl<TextBlock>(prefix + "Artist")!.Text = Read("Artist");
        this.FindControl<TextBlock>(prefix + "Album")!.Text = Read("Album");
        SetUiText(prefix + "Playback", Read("State"));
        this.FindControl<TextBlock>(prefix + "Time")!.Text = Read("Elapsed") + " / " + Read("Duration");
        if (id == "Winamp")
        {
            string FormatValue(string key) => string.IsNullOrWhiteSpace(Read(key)) ? "-" : Read(key);
            this.FindControl<TextBlock>("WinampMediaPlaylist")!.Text =
                FormatValue("PlaylistPosition") + " / " + FormatValue("PlaylistCount");
            this.FindControl<TextBlock>("WinampMediaAudio")!.Text =
                FormatValue("BitrateKbps") + " kbps / " + FormatValue("SampleRateHz") + " Hz";
        }
        SetUiText(prefix + "Telemetry", $"VU: {Read("Vu")}  Spectrum: {Read("Spectrum")}");
    }

    private static UiMessage MediaErrorMessage(string provider, Exception exception) =>
        provider == "Media" && exception.GetBaseException() is System.ComponentModel.Win32Exception { NativeErrorCode: 2 }
            ? "playerctl is missing. Install playerctl to enable Linux media."
            : exception.GetBaseException().Message;

    private async Task CreateMediaPageAsync(string sourceId, string title)
    {
        await AddSourcePageAsync(page =>
        {
            page.Name = title;
            page.Line1Template = "{" + sourceId + ".Title}";
            page.Line2Template = "{" + sourceId + ".Artist}";
        });
    }

    private async Task StopMediaAsync()
    {
        _mediaStopping = true;
        _mediaTimer?.Stop();
        _mediaTimer = null;
        _mediaCancellation.Cancel();
        await _mediaLifetime.StopAsync(() => RuntimeShutdown.RunAsync(
            () => _mediaRefresh,
            () => _windowsMedia.DisposeAsync().AsTask(),
            () => _mediaWinamp.StopAsync(),
            () => _mediaWinamp.DisposeAsync().AsTask()));
    }
}
