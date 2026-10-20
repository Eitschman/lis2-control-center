using Avalonia.Controls;
using Avalonia.Threading;
using LIS2.Winamp;
using LIS2.WmpLegacy;
using LIS2.Runtime;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly WinampDataSource _mediaWinamp = new();
    private readonly WmpLegacyDataSource _mediaLegacy = new();
    private DispatcherTimer? _mediaTimer;
    private readonly LinuxMprisSource _mpris = new();
    private readonly WindowsMediaBridge _windowsMedia = new();
    private bool _mprisRefreshing;
    private bool _mediaStopping;
    private readonly AsyncProviderLifetime _mediaLifetime = new();
    private Task _mediaRefresh = Task.CompletedTask;
    private readonly CancellationTokenSource _mediaCancellation = new();
    private readonly Dictionary<string, string> _mediaErrors = new();

    private void InitializeMedia()
    {
        this.FindControl<Border>("LinuxMediaCard")!.IsVisible = OperatingSystem.IsLinux();
        this.FindControl<Border>("WinampMediaCard")!.IsVisible = OperatingSystem.IsWindows();
        this.FindControl<Border>("WmpLegacyMediaCard")!.IsVisible = OperatingSystem.IsWindows();
        this.FindControl<Border>("WindowsMediaCard")!.IsVisible = OperatingSystem.IsWindows();
        _mediaTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _mediaTimer.Tick += (_, _) => RefreshMedia();
        if (_startBackgroundSources) _mediaTimer.Start();
        this.FindControl<Button>("WindowsMediaPage")!.Click += async (_, _) => await CreateMediaPageAsync("WindowsMedia", "Windows Media Player");
        this.FindControl<Button>("WinampMediaPage")!.Click += async (_, _) =>
            await CreateMediaPageAsync("Winamp", "Winamp");
        this.FindControl<Button>("LinuxMediaPage")!.Click += async (_, _) => await CreateMediaPageAsync("Media", "Linux media");
        this.FindControl<Button>("WmpLegacyMediaPage")!.Click += async (_, _) =>
            await CreateMediaPageAsync("WmpLegacy", "Windows Media Player Legacy");
        if (_startBackgroundSources) _ = _mediaLifetime.StartAsync(StartMediaAsync);
    }

    private async Task StartMediaAsync()
    {
        // WPF plug-ins use Windows named-pipe servers; do not activate on Linux.
        if (!OperatingSystem.IsWindows())
        {
            RefreshMedia();
            return;
        }
        await StartMediaProviderSafeAsync("WindowsMedia", () => _windowsMedia.StartAsync());
        if (_mediaStopping) return;
        this.FindControl<Border>("WindowsMediaCard")!.IsVisible = true;
        await StartMediaProviderSafeAsync("Winamp", () => _mediaWinamp.StartAsync());
        await StartMediaProviderSafeAsync("WmpLegacy", () => _mediaLegacy.StartAsync());
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
            System.Diagnostics.Trace.TraceError("Media startup {0} failed: {1}", provider, ex);
            if (_mediaStopping) return;
            _pageSources.UpdateMedia(provider, new Dictionary<string, object?>());
            var controlName = provider switch
            {
                "WindowsMedia" => "WindowsMediaStatus",
                "Winamp" => "WinampMediaStatus",
                "Media" => "LinuxMediaStatus",
                _ => "WmpLegacyMediaStatus"
            };
            var status = this.FindControl<TextBlock>(controlName);
            if (status is not null)
            {
                status.Text = "Provider startup error: " + MediaErrorMessage(provider, ex);
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
        RefreshMediaProvider("WmpLegacy", () => RefreshMediaSource("WmpLegacy", _mediaLegacy.Values));
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
            System.Diagnostics.Trace.TraceError("Media provider {0} failed: {1}", provider, ex);
            var status = provider switch
            {
                "WindowsMedia" => "WindowsMediaStatus",
                "Winamp" => "WinampMediaStatus",
                _ => "WmpLegacyMediaStatus"
            };
            _pageSources.UpdateMedia(provider, new Dictionary<string, object?>());
            var statusControl = this.FindControl<TextBlock>(status);
            if (statusControl is not null)
                statusControl.Text = "Provider error: " + message;
        }
    }

    private void RefreshWindowsMedia()
    {
        var values = _windowsMedia.Values;
        _pageSources.UpdateMedia("WindowsMedia", values);
        string Read(string key) => values.TryGetValue(key, out var value) ? Convert.ToString(value) ?? "" : "";
        this.FindControl<TextBlock>("WindowsMediaStatus")!.Text = Read("Connected") == "True" ? "Connected" : "Disconnected";
        this.FindControl<TextBlock>("WindowsMediaTitle")!.Text = Read("Title");
        this.FindControl<TextBlock>("WindowsMediaArtist")!.Text = Read("Artist");
        this.FindControl<TextBlock>("WindowsMediaAlbum")!.Text = Read("Album");
        this.FindControl<TextBlock>("WindowsMediaPlayback")!.Text = Read("State");
        this.FindControl<TextBlock>("WindowsMediaTime")!.Text = Read("Elapsed") + " / " + Read("Duration");
    }

    private async Task RefreshMprisAsync()
    {
        if (_mprisRefreshing) return;
        _mprisRefreshing = true;
        try
        {
            var values = await _mpris.ReadAsync(_mediaCancellation.Token);
            if (_mediaStopping) return;
            _pageSources.UpdateMedia("Media", values);
            string Read(string key) => values.TryGetValue(key, out var value) ? Convert.ToString(value) ?? "" : "";
            this.FindControl<TextBlock>("LinuxMediaStatus")!.Text = Read("Connected") == "True" ? "Connected" : "Disconnected";
            this.FindControl<TextBlock>("LinuxMediaTitle")!.Text = Read("Title");
            this.FindControl<TextBlock>("LinuxMediaArtist")!.Text = Read("Artist");
            this.FindControl<TextBlock>("LinuxMediaAlbum")!.Text = Read("Album");
            this.FindControl<TextBlock>("LinuxMediaPlayback")!.Text = Read("State");
            this.FindControl<TextBlock>("LinuxMediaDuration")!.Text = Read("Duration");
        }
        catch (Exception ex)
        {
            if (_mediaStopping) return;
            _applicationJournal.Record("Media", "MPRIS refresh failed (" + ex.GetBaseException().GetType().Name + ")");
            System.Diagnostics.Trace.TraceError("Linux MPRIS refresh failed: {0}", ex);
            _pageSources.UpdateMedia("Media", new Dictionary<string, object?>());
            var status = this.FindControl<TextBlock>("LinuxMediaStatus");
            if (status is not null) status.Text = "Provider error: " + MediaErrorMessage("Media", ex);
        }
        finally { _mprisRefreshing = false; }
    }

    private void RefreshMediaSource(string id, IReadOnlyDictionary<string, object?> values)
    {
        _pageSources.UpdateMedia(id, values);
        var prefix = id == "Winamp" ? "WinampMedia" : "WmpLegacyMedia";
        string Read(string key) => values.TryGetValue(key, out var value) ? Convert.ToString(value) ?? "" : "";
        this.FindControl<TextBlock>(prefix + "Status")!.Text = Read("Connected") == "True" ? "Connected" : "Disconnected";
        this.FindControl<TextBlock>(prefix + "Title")!.Text = string.IsNullOrWhiteSpace(Read("Title")) ? "Nothing playing" : Read("Title");
        this.FindControl<TextBlock>(prefix + "Artist")!.Text = Read("Artist");
        this.FindControl<TextBlock>(prefix + "Album")!.Text = Read("Album");
        this.FindControl<TextBlock>(prefix + "Playback")!.Text = Read("State");
        this.FindControl<TextBlock>(prefix + "Time")!.Text = Read("Elapsed") + " / " + Read("Duration");
        if (id == "Winamp")
        {
            string FormatValue(string key) => string.IsNullOrWhiteSpace(Read(key)) ? "-" : Read(key);
            this.FindControl<TextBlock>("WinampMediaPlaylist")!.Text =
                FormatValue("PlaylistPosition") + " / " + FormatValue("PlaylistCount");
            this.FindControl<TextBlock>("WinampMediaAudio")!.Text =
                FormatValue("BitrateKbps") + " kbps / " + FormatValue("SampleRateHz") + " Hz";
        }
        this.FindControl<TextBlock>(prefix + "Telemetry")!.Text = "VU: " + Read("Vu") + "  Spectrum: " + Read("Spectrum");
    }

    private static string MediaErrorMessage(string provider, Exception exception) =>
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
            () => _mediaWinamp.StopAsync(), () => _mediaLegacy.StopAsync(),
            () => _mediaWinamp.DisposeAsync().AsTask(), () => _mediaLegacy.DisposeAsync().AsTask()));
    }
}
