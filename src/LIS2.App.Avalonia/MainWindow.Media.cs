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
    private bool _mprisRefreshing;

    private void InitializeMedia()
    {
        this.FindControl<Border>("LinuxMediaCard")!.IsVisible = OperatingSystem.IsLinux();
        this.FindControl<Border>("WinampMediaCard")!.IsVisible = OperatingSystem.IsWindows();
        this.FindControl<Border>("WmpLegacyMediaCard")!.IsVisible = OperatingSystem.IsWindows();
        this.FindControl<Border>("WindowsMediaCard")!.IsVisible = OperatingSystem.IsWindows();
        _mediaTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _mediaTimer.Tick += (_, _) => RefreshMedia();
        _mediaTimer.Start();
        this.FindControl<Button>("WinampMediaPage")!.Click += (_, _) =>
            CreateMediaPage("Winamp", "Winamp");
        this.FindControl<Button>("LinuxMediaPage")!.Click += (_, _) => CreateMediaPage("Media", "Linux media");
        this.FindControl<Button>("WmpLegacyMediaPage")!.Click += (_, _) =>
            CreateMediaPage("WmpLegacy", "Windows Media Player Legacy");
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
        try { await _mediaWinamp.StartAsync(); }
        catch (Exception ex) { this.FindControl<TextBlock>("WinampMediaStatus")!.Text = ex.Message; }
        try { await _mediaLegacy.StartAsync(); }
        catch (Exception ex) { this.FindControl<TextBlock>("WmpLegacyMediaStatus")!.Text = ex.Message; }
        RefreshMedia();
    }

    private void RefreshMedia()
    {
        if (!OperatingSystem.IsWindows())
        {
            _ = RefreshMprisAsync();
            return;
        }
        RefreshMediaSource("Winamp", _mediaWinamp.Values);
        RefreshMediaSource("WmpLegacy", _mediaLegacy.Values);
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
        this.FindControl<TextBlock>(prefix + "Telemetry")!.Text = "VU: " + Read("Vu") + "  Spectrum: " + Read("Spectrum");
    }

    private void CreateMediaPage(string sourceId, string title)
    {
        var page = _pages.Add();
        page.Name = title;
        page.Line1Template = "{" + sourceId + ".Title}";
        page.Line2Template = "{" + sourceId + ".Artist}";
        RefreshPageList(_pages.Pages.Count - 1);
        _ = SavePortableWorkspaceAsync();
        NavigateTo(2);
    }

    private async Task StopMediaAsync()
    {
        _mediaTimer?.Stop();
        await _mediaWinamp.StopAsync();
        await _mediaLegacy.StopAsync();
        await _mediaWinamp.DisposeAsync();
        await _mediaLegacy.DisposeAsync();
    }
}
