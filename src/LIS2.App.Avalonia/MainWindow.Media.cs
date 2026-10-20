using Avalonia.Controls;
using Avalonia.Threading;
using LIS2.Winamp;
using LIS2.WmpLegacy;

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

    private void InitializeMedia()
    {
        this.FindControl<Border>("LinuxMediaCard")!.IsVisible = OperatingSystem.IsLinux();
        this.FindControl<Border>("WinampMediaCard")!.IsVisible = OperatingSystem.IsWindows();
        this.FindControl<Border>("WmpLegacyMediaCard")!.IsVisible = OperatingSystem.IsWindows();
        this.FindControl<Border>("WindowsMediaCard")!.IsVisible = OperatingSystem.IsWindows();
        _mediaTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _mediaTimer.Tick += (_, _) => RefreshMedia();
        _mediaTimer.Start();
        this.FindControl<Button>("WindowsMediaPage")!.Click += async (_, _) => await CreateMediaPageAsync("WindowsMedia", "Windows Media Player");
        this.FindControl<Button>("WinampMediaPage")!.Click += async (_, _) =>
            await CreateMediaPageAsync("Winamp", "Winamp");
        this.FindControl<Button>("LinuxMediaPage")!.Click += async (_, _) => await CreateMediaPageAsync("Media", "Linux media");
        this.FindControl<Button>("WmpLegacyMediaPage")!.Click += async (_, _) =>
            await CreateMediaPageAsync("WmpLegacy", "Windows Media Player Legacy");
        _ = StartMediaAsync();
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
        this.FindControl<Border>("WindowsMediaCard")!.IsVisible = _windowsMedia.IsAvailable;
        await StartMediaProviderSafeAsync("Winamp", () => _mediaWinamp.StartAsync());
        await StartMediaProviderSafeAsync("WmpLegacy", () => _mediaLegacy.StartAsync());
        if (!_mediaStopping) RefreshMedia();
    }

    private async Task StartMediaProviderSafeAsync(string provider, Func<Task> start)
    {
        if (_mediaStopping) return;
        try { await start(); }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("Media startup {0} failed: {1}", provider, ex);
            if (_mediaStopping) return;
            _pageSources.UpdateMedia(provider, new Dictionary<string, object?>());
            var controlName = provider switch
            {
                "WindowsMedia" => "WindowsMediaStatus",
                "Winamp" => "WinampMediaStatus",
                _ => "WmpLegacyMediaStatus"
            };
            var status = this.FindControl<TextBlock>(controlName);
            if (status is not null) status.Text = "Provider startup error: " + ex.GetBaseException().Message;
        }
    }

    private void RefreshMedia()
    {
        if (_mediaStopping) return;
        if (!OperatingSystem.IsWindows())
        {
            _ = RefreshMprisAsync();
            return;
        }
        RefreshMediaProvider("WindowsMedia", RefreshWindowsMedia);
        RefreshMediaProvider("Winamp", () => RefreshMediaSource("Winamp", _mediaWinamp.Values));
        RefreshMediaProvider("WmpLegacy", () => RefreshMediaSource("WmpLegacy", _mediaLegacy.Values));
    }

    private void RefreshMediaProvider(string provider, Action refresh)
    {
        try
        {
            refresh();
        }
        catch (Exception ex)
        {
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
            var values = await _mpris.ReadAsync();
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
            System.Diagnostics.Trace.TraceError("Linux MPRIS refresh failed: {0}", ex);
            _pageSources.UpdateMedia("Media", new Dictionary<string, object?>());
            var status = this.FindControl<TextBlock>("LinuxMediaStatus");
            if (status is not null) status.Text = "Provider error: " + ex.GetBaseException().Message;
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

    private async Task CreateMediaPageAsync(string sourceId, string title)
    {
        var page = _pages.Add();
        page.Name = title;
        page.Line1Template = "{" + sourceId + ".Title}";
        page.Line2Template = "{" + sourceId + ".Artist}";
        RefreshPageList(_pages.Pages.Count - 1);
        await ReloadRunningPagesAsync();
        await SavePortableWorkspaceAsync();
        NavigateTo(2);
    }

    private async Task StopMediaAsync()
    {
        _mediaStopping = true;
        _mediaTimer?.Stop();
        _mediaTimer = null;
        await _windowsMedia.DisposeAsync();
        await _mediaWinamp.StopAsync();
        await _mediaLegacy.StopAsync();
        await _mediaWinamp.DisposeAsync();
        await _mediaLegacy.DisposeAsync();
    }
}
