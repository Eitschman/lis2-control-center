using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private bool _checkUpdatesAutomatically;
    private bool _installUpdatesAutomatically;
    private bool _debianUpdates = true;
    private bool _updatingControls;
    private bool _updateBusy;
    private bool _systemInstallerRunning;
    private bool _updatesStarted;
    private AvailableUpdate? _availableUpdate;
    private string? _downloadedUpdate;
    private string? _downloadedChecksum;
    private string _updateStatus = "Not checked.";
    private string? _updateError;
    private CancellationTokenSource? _updateOperation;
    private readonly CancellationTokenSource _updateLifetime = new();
    private readonly HttpClient _updateClient;
    private readonly DispatcherTimer _updateCheckTimer = new() { Interval = TimeSpan.FromDays(1) };

    private string UpdateLabel(string text) => UpdateText.Get(text, CurrentUiLanguage());

    /// <summary>Tests und Desktop-Smokes starten keine externen Updateprüfungen.</summary>
    private void InitializeUpdates()
    {
        if (UpdateStartup.ResultFile is { } result && File.Exists(result))
        {
            _updateStatus = "Update restored the previous version: {0}";
            _updateError = File.ReadAllText(result);
        }
        this.FindControl<Button>("UpdateCheck")!.Click += async (_, _) => await CheckUpdatesAsync(false);
        this.FindControl<Button>("UpdateDownload")!.Click += async (_, _) => await DownloadUpdateAsync();
        this.FindControl<Button>("UpdateInstall")!.Click += async (_, _) => await InstallUpdateAsync();
        this.FindControl<Button>("UpdateCancel")!.Click += (_, _) => _updateOperation?.Cancel();
        this.FindControl<Button>("UpdateFolder")!.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(Path.GetDirectoryName(_downloadedUpdate!)!) { UseShellExecute = true })?.Dispose(); }
            catch (Exception error) { SetUpdateError(error); }
        };
        this.FindControl<Button>("UpdateNotice")!.Click += (_, _) => NavigateTo(7);
        this.FindControl<CheckBox>("UpdateAutomaticCheck")!.IsCheckedChanged += async (_, _) =>
        {
            if (_updatingControls) return;
            _checkUpdatesAutomatically = this.FindControl<CheckBox>("UpdateAutomaticCheck")!.IsChecked == true;
            if (!_checkUpdatesAutomatically) _installUpdatesAutomatically = false;
            RefreshUpdates();
            await SavePortableWorkspaceAsync();
        };
        this.FindControl<CheckBox>("UpdateAutomaticInstall")!.IsCheckedChanged += async (_, _) =>
        {
            if (_updatingControls) return;
            _installUpdatesAutomatically = this.FindControl<CheckBox>("UpdateAutomaticInstall")!.IsChecked == true;
            await SavePortableWorkspaceAsync();
        };
        this.FindControl<ComboBox>("UpdatePackageKind")!.SelectionChanged += async (_, _) =>
        {
            if (_updatingControls) return;
            _debianUpdates = this.FindControl<ComboBox>("UpdatePackageKind")!.SelectedIndex == 0;
            _availableUpdate = null;
            _downloadedUpdate = null;
            _updateStatus = "Not checked.";
            RefreshUpdates();
            await SavePortableWorkspaceAsync();
        };
        UiLanguageChanged += RefreshUpdates;
        _updateCheckTimer.Tick += async (_, _) => await CheckUpdatesAsync(true);
        Opened += (_, _) =>
        {
            if (!_startBackgroundSources || _updatesStarted) return;
            _updatesStarted = true;
            _updateCheckTimer.Start();
            _ = CheckUpdatesAsync(true);
        };
        RefreshUpdates();
    }

    internal void RefreshUpdates()
    {
        _updatingControls = true;
        try
        {
            foreach (var (name, text) in new[] { ("UpdateCheck", "Check for updates"), ("UpdateDownload", "Download update"),
                ("UpdateCancel", "Cancel"), ("UpdateFolder", "Open download folder"), ("UpdateNotice", "Update available") })
                this.FindControl<Button>(name)!.Content = UpdateLabel(text);
            this.FindControl<TextBlock>("UpdateHeading")!.Text = UpdateLabel("Updates");
            this.FindControl<Expander>("UpdateNotesExpander")!.Header = UpdateLabel("Release notes");
            this.FindControl<CheckBox>("UpdateAutomaticCheck")!.Content = UpdateLabel("Check automatically");
            var install = this.FindControl<CheckBox>("UpdateAutomaticInstall")!;
            install.Content = UpdateLabel("Install automatically (Windows; restarts the application)");
            install.IsVisible = OperatingSystem.IsWindows();
            install.IsEnabled = _checkUpdatesAutomatically && !_updateBusy;
            install.IsChecked = _installUpdatesAutomatically;
            this.FindControl<CheckBox>("UpdateAutomaticCheck")!.IsChecked = _checkUpdatesAutomatically;
            this.FindControl<CheckBox>("UpdateAutomaticCheck")!.IsEnabled = !_updateBusy;
            var kind = this.FindControl<ComboBox>("UpdatePackageKind")!;
            kind.IsVisible = UpdateInstallation.DebianAvailable;
            kind.IsEnabled = !_updateBusy;
            ((ComboBoxItem)kind.Items[0]!).Content = UpdateLabel("Debian package");
            ((ComboBoxItem)kind.Items[1]!).Content = UpdateLabel("Portable archive");
            kind.SelectedIndex = _debianUpdates ? 0 : 1;
            this.FindControl<TextBlock>("UpdateVersions")!.Text = _availableUpdate is null
                ? string.Format(UpdateLabel("Installed: {0}"), ApplicationUpdates.InstalledVersion)
                : string.Format(UpdateLabel("Installed: {0} · Available: {1}"), ApplicationUpdates.InstalledVersion, _availableUpdate.Version);
            this.FindControl<TextBlock>("UpdateStatus")!.Text = _updateError is null ? UpdateLabel(_updateStatus)
                : string.Format(UpdateLabel(_updateStatus), _updateError);
            var newer = _availableUpdate?.Version > ApplicationUpdates.InstalledVersion;
            this.FindControl<Button>("UpdateNotice")!.IsVisible = newer;
            this.FindControl<Button>("UpdateCheck")!.IsEnabled = !_updateBusy;
            this.FindControl<Button>("UpdateDownload")!.IsVisible = newer && _downloadedUpdate is null;
            this.FindControl<Button>("UpdateDownload")!.IsEnabled = !_updateBusy;
            var installButton = this.FindControl<Button>("UpdateInstall")!;
            installButton.Content = UpdateLabel(OperatingSystem.IsWindows() ? "Install and restart" : "Open system installer");
            installButton.IsVisible = _downloadedUpdate is not null && (OperatingSystem.IsWindows() || _downloadedUpdate.EndsWith(".deb", StringComparison.Ordinal));
            installButton.IsEnabled = !_updateBusy;
            this.FindControl<Button>("UpdateFolder")!.IsVisible = _downloadedUpdate is not null;
            this.FindControl<Button>("UpdateCancel")!.IsVisible = _updateBusy && !_systemInstallerRunning;
            this.FindControl<ProgressBar>("UpdateProgress")!.IsVisible = _updateBusy && !_systemInstallerRunning;
            this.FindControl<Expander>("UpdateNotesExpander")!.IsVisible = _availableUpdate is not null;
            this.FindControl<TextBox>("UpdateNotes")!.Text = _availableUpdate?.Notes;
        }
        finally { _updatingControls = false; }
    }

    private void SetUpdateError(Exception error)
    {
        _updateStatus = "Update failed: {0}";
        _updateError = error.Message;
        RefreshUpdates();
    }

    private async Task RunUpdateOperationAsync(string status, Func<CancellationToken, Task> action)
    {
        if (_updateBusy || _windowClosed) return;
        _updateBusy = true;
        _updateError = null;
        _updateStatus = status;
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(_updateLifetime.Token);
        _updateOperation = operation;
        this.FindControl<ProgressBar>("UpdateProgress")!.IsIndeterminate = true;
        this.FindControl<ProgressBar>("UpdateProgress")!.Value = 0;
        RefreshUpdates();
        try { await action(operation.Token); }
        catch (OperationCanceledException) { _updateStatus = "Cancelled."; }
        catch (Exception error) { SetUpdateError(error); }
        finally
        {
            _updateOperation = null;
            _updateBusy = false;
            RefreshUpdates();
        }
    }

    private async Task CheckUpdatesAsync(bool automatic)
    {
        if (automatic && (!_checkUpdatesAutomatically || !_startBackgroundSources)) return;
        var succeeded = false;
        await RunUpdateOperationAsync("Checking…", async token =>
        {
            _downloadedUpdate = null;
            _availableUpdate = null;
            _availableUpdate = await new ApplicationUpdates(_updateClient).CheckAsync(OperatingSystem.IsWindows(), RuntimeInformation.OSArchitecture,
                _debianUpdates && UpdateInstallation.DebianAvailable, token);
            _updateStatus = _availableUpdate.Version > ApplicationUpdates.InstalledVersion ? "Update available." : "This version is up to date.";
            succeeded = true;
        });
        if (succeeded && automatic && OperatingSystem.IsWindows() && _installUpdatesAutomatically
            && _availableUpdate!.Version > ApplicationUpdates.InstalledVersion)
        {
            await DownloadUpdateAsync();
            if (_downloadedUpdate is not null) await InstallUpdateAsync();
        }
    }

    private Task DownloadUpdateAsync() => RunUpdateOperationAsync("Downloading and verifying…", async token =>
    {
        if (_availableUpdate is null) throw new InvalidOperationException("Check for updates first.");
        var progress = this.FindControl<ProgressBar>("UpdateProgress")!;
        progress.IsIndeterminate = false;
        var download = await new ApplicationUpdates(_updateClient).DownloadAsync(_availableUpdate,
            new Progress<double>(value => progress.Value = value), token);
        _downloadedUpdate = download.Path;
        _downloadedChecksum = download.Sha256;
        _updateStatus = "Download verified.";
    });

    private Task InstallUpdateAsync() => RunUpdateOperationAsync("Preparing installation…", async token =>
    {
        if (_downloadedUpdate is null || _availableUpdate is null) throw new InvalidOperationException("Download an update first.");
        await ApplicationUpdates.VerifyAsync(_downloadedUpdate, _downloadedChecksum!, token);
        if (OperatingSystem.IsWindows())
        {
            var plan = await Task.Run(() => UpdateInstallation.PrepareWindows(_downloadedUpdate, _availableUpdate.Version), token);
            token.ThrowIfCancellationRequested();
            using var helper = UpdateInstallation.StartWindowsHelper(plan);
            await SavePortableWorkspaceAsync(true);
            await BeginShutdownAsync();
            await File.WriteAllTextAsync(Path.Combine(plan.Session, "armed"), "shutdown drained", CancellationToken.None);
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.Shutdown();
        }
        else
        {
            _systemInstallerRunning = true;
            try
            {
                _updateStatus = "System installer opened; confirm installation there, then restart LIS2.";
                RefreshUpdates();
                await UpdateInstallation.OpenLinuxPackageAsync(_downloadedUpdate, token);
            }
            finally { _systemInstallerRunning = false; }
        }
    });
}
