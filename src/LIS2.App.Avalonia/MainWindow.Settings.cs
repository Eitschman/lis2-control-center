using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Microsoft.Win32;
using System.Diagnostics;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private bool _loadingSettings;
    private bool _settingsTransportBusy;
    private bool _applyingTheme;
    private const string StartupKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValue = "LIS2 Control Center";

    private void InitializeSettings()
    {
        _loadingSettings = true;
        var mode = this.FindControl<ComboBox>("TransportModeComboBox")!;
        var port = this.FindControl<ComboBox>("SettingsPortComboBox")!;
        mode.SelectionChanged += (_, _) => port.IsEnabled = mode.SelectedIndex == 1;
        port.IsEnabled = mode.SelectedIndex == 1;
        this.FindControl<Button>("SettingsRefreshPorts")!.Click += (_, _) => RefreshSettingsPorts();
        this.FindControl<Button>("SettingsApplyTransport")!.Click += async (_, _) => await ApplySettingsTransportAsync();
        this.FindControl<Button>("SettingsDisconnectTransport")!.Click += async (_, _) => await DisconnectSelectedTransportAsync();

        var theme = this.FindControl<ComboBox>("ThemeModeComboBox")!;
        theme.SelectionChanged += (_, _) =>
        {
            if (_loadingSettings) return;
            _preferredTheme = theme.SelectedIndex switch { 1 => "Light", 2 => "Dark", _ => "System" };
            ApplyPreferredTheme();
            _ = SavePortableWorkspaceAsync();
        };
        var language = this.FindControl<ComboBox>("LanguageModeComboBox")!;
        language.SelectionChanged += (_, _) =>
        {
            if (_loadingSettings) return;
            _preferredLanguage = language.SelectedIndex switch
            {
                1 => "English",
                2 => "German",
                3 => "French",
                4 => "Turkish",
                5 => "Russian",
                _ => "System"
            };
            ApplySettingsLocalization();
            this.FindControl<TextBlock>("SettingsAppearanceStatus")!.Text = string.Empty;
            _ = SavePortableWorkspaceAsync();
        };
        this.FindControl<Border>("SettingsWindowsCard")!.IsVisible = OperatingSystem.IsWindows() || OperatingSystem.IsLinux();
        var startup = this.FindControl<CheckBox>("StartWithWindowsCheckBox")!;
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
        {
            var linuxStartup = DesktopAutostart.ForCurrentUser();
            try { startup.IsChecked = OperatingSystem.IsWindows() ? IsWindowsStartupEnabled() : linuxStartup.IsEnabled; }
            catch (Exception ex) { SetStartupStatus(ex.Message); }
            startup.IsCheckedChanged += (_, _) =>
            {
                if (_loadingSettings) return;
                try
                {
                    if (OperatingSystem.IsWindows()) SetWindowsStartup(startup.IsChecked == true);
                    else
                    {
                        var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Executable path unavailable.");
                        linuxStartup.SetEnabled(startup.IsChecked == true, executable,
                            string.Equals(Path.GetFileNameWithoutExtension(executable), "dotnet", StringComparison.OrdinalIgnoreCase)
                                ? typeof(MainWindow).Assembly.Location : null);
                    }
                    SetStartupStatus(startup.IsChecked == true ? "Desktop startup enabled." : "Desktop startup disabled.");
                }
                catch (Exception ex)
                {
                    _loadingSettings = true;
                    startup.IsChecked = !startup.IsChecked;
                    _loadingSettings = false;
                    SetStartupStatus($"Desktop startup error: {(UiMessage)ex.Message}");
                }
            };
        }
        LoadSettingsPreferences();
        if (Application.Current is { } app) app.ActualThemeVariantChanged += OnSystemThemeChanged;
        Closed += (_, _) => { if (Application.Current is { } app) app.ActualThemeVariantChanged -= OnSystemThemeChanged; };
        _loadingSettings = false;
    }

    private void LoadSettingsPreferences()
    {
        _loadingSettings = true;
        this.FindControl<ComboBox>("TransportModeComboBox")!.SelectedIndex = SerialSelected ? 1 : 0;
        RefreshSettingsPorts(true);
        this.FindControl<ComboBox>("ThemeModeComboBox")!.SelectedIndex =
            _preferredTheme.Equals("Light", StringComparison.OrdinalIgnoreCase) ? 1 :
            _preferredTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
        this.FindControl<ComboBox>("LanguageModeComboBox")!.SelectedIndex =
            _preferredLanguage.ToLowerInvariant() switch
            {
                "english" => 1,
                "german" => 2,
                "french" => 3,
                "turkish" => 4,
                "russian" => 5,
                _ => 0
            };
        ApplyPreferredTheme();
        _loadingSettings = false;
        ApplySettingsLocalization();
    }

    private void ApplyPreferredTheme()
    {
        if (_applyingTheme) return;
        if (Application.Current is not { } app) return;
        _applyingTheme = true;
        try
        {
            app.RequestedThemeVariant = _preferredTheme switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
            var dark = _preferredTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ||
                (_preferredTheme.Equals("System", StringComparison.OrdinalIgnoreCase) &&
                 app.ActualThemeVariant == ThemeVariant.Dark);
            static global::Avalonia.Media.SolidColorBrush Brush(string hex) =>
                new(global::Avalonia.Media.Color.Parse(hex));
            app.Resources["Lis2CardBrush"] = Brush(dark ? "#202A35" : "#FFFFFF");
            app.Resources["Lis2SurfaceBrush"] = Brush(dark ? "#141C26" : "#F5F7FA");
            app.Resources["Lis2BorderBrush"] = Brush(dark ? "#455466" : "#D4DCE5");
            app.Resources["Lis2SidebarBrush"] = Brush(dark ? "#192430" : "#FFFFFF");
            app.Resources["Lis2SidebarTextBrush"] = Brush(dark ? "#EDF3FA" : "#10243A");
            app.Resources["Lis2SelectedBrush"] = Brush(dark ? "#26583E" : "#DDF6E7");
            app.Resources["Lis2IconBrush"] = Brush(dark ? "#C7D7E6" : "#233D55");
            app.Resources["Lis2MutedCardBrush"] = Brush(dark ? "#263444" : "#F8FAFC");
            app.Resources["Lis2HeaderBrush"] = Brush(dark ? "#304154" : "#E8EEF3");
            app.Resources["Lis2StatusBrush"] = Brush(dark ? "#194A32" : "#E8F5EC");
            app.Resources["Lis2StatusTextBrush"] = Brush(dark ? "#B4F2CD" : "#166534");
            UpdateSection();
        }
        finally { _applyingTheme = false; }
    }

    private void OnSystemThemeChanged(object? sender, EventArgs args)
    {
        if (_preferredTheme.Equals("System", StringComparison.OrdinalIgnoreCase)) ApplyPreferredTheme();
    }

    private void RefreshSettingsPorts(bool restorePreference = false)
    {
        try
        {
            var picker = this.FindControl<ComboBox>("SettingsPortComboBox")!;
            var previous = restorePreference ? _preferredPortName : picker.SelectedItem as string ?? _preferredPortName;
            var ports = SerialPortDiscovery.GetAvailablePorts();
            // Ein gespeicherter, gerade fehlender Port darf nicht durch einen anderen ersetzt werden.
            var choices = ports.ToList();
            if (!string.IsNullOrWhiteSpace(previous) && !choices.Contains(previous)) choices.Add(previous);
            picker.ItemsSource = choices;
            picker.SelectedItem = previous ?? ports.FirstOrDefault();
            SetUiText("SettingsTransportStatus", ports.Count == 0 ? "No serial ports found." : (UiMessage)$"{ports.Count} serial port(s) found.");
        }
        catch (Exception ex)
        {
            SetUiText("SettingsTransportStatus", $"Port discovery failed: {(UiMessage)ex.Message}");
        }
    }

    private async Task ApplySettingsTransportAsync()
    {
        if (_settingsTransportBusy || _serialBusy || _busy || _dashboardFansBusy) return;
        var status = this.FindControl<TextBlock>("SettingsTransportStatus")!;
        var serial = this.FindControl<ComboBox>("TransportModeComboBox")!.SelectedIndex == 1;
        var port = this.FindControl<ComboBox>("SettingsPortComboBox")!.SelectedItem as string;
        if (serial && string.IsNullOrWhiteSpace(port))
        {
            SetUiText(status, "Select a serial port first.");
            return;
        }
        _settingsTransportBusy = true;
        UpdateVirtualControls();
        try
        {
            // Erst speichern, dann die Verbindung wechseln: ein Schreibfehler verändert kein Gerät.
            var beforeMode = _preferredTransportMode;
            var beforePort = _preferredPortName;
            _preferredTransportMode = serial ? "Serial" : "Virtual";
            _preferredPortName = port;
            try { await SavePortableWorkspaceAsync(true); }
            catch { _preferredTransportMode = beforeMode; _preferredPortName = beforePort; throw; }
            await DisconnectTransportsAsync();
            if (serial)
            {
                await ConnectSerialAsync(port);
                if (_serialDisplay?.IsConnected == true) await StartSerialPlaybackAsync();
            }
            else
            {
                await RunVirtualActionAsync(ConnectWithGlyphsAsync);
                if (_virtualDevice.IsConnected) await StartPagePlaybackAsync();
            }
            SetUiText(status, SelectedDisplay?.IsConnected == true
                ? serial ? (UiMessage)$"LIS2 connected: {port}" : "Virtual LIS2 connected."
                : (UiMessage)$"Connection failed: {this.FindControl<TextBlock>(serial ? "SerialStatus" : "VirtualStatus")!.Text}");
        }
        catch (Exception ex) { SetUiText(status, $"Connection error: {(UiMessage)ex.Message}"); }
        finally { _settingsTransportBusy = false; UpdateVirtualControls(); UpdateSerialControls(); }
    }

    /// <summary>Verbindungswechsel drainiert beide Ausgabeläufe und verwirft jede Lüfterfreigabe.</summary>
    private async Task DisconnectTransportsAsync()
    {
        this.FindControl<CheckBox>("FanHardwareConsent")!.IsChecked = false;
        this.FindControl<CheckBox>("DashboardFanConsent")!.IsChecked = false;
        StopPagePlayback();
        await _pageTimer.StopAndDrainAsync();
        if (_virtualSequence is not null) await _virtualSequence.StopAndDrainAsync();
        await CloseSerialDisplayAsync();
        if (_virtualDevice.IsConnected) await _virtualDevice.DisconnectAsync();
        UpdatePreview();
    }

    private async Task DisconnectSelectedTransportAsync()
    {
        if (_settingsTransportBusy || _serialBusy || _busy || _dashboardFansBusy) return;
        _settingsTransportBusy = true;
        UpdateVirtualControls();
        try
        {
            await DisconnectTransportsAsync();
            SetUiText("SettingsTransportStatus", "LIS2 disconnected.");
        }
        catch (Exception ex) { SetVirtualStatus($"Disconnect failed: {(UiMessage)ex.Message}"); }
        finally { _settingsTransportBusy = false; UpdateVirtualControls(); UpdateSerialControls(); }
    }

    private static bool IsWindowsStartupEnabled()
    {
        if (!OperatingSystem.IsWindows()) return false;
        using var key = Registry.CurrentUser.OpenSubKey(StartupKey);
        return key?.GetValue(StartupValue) is string;
    }

    private static void SetWindowsStartup(bool enabled)
    {
        if (!OperatingSystem.IsWindows()) return;
        using var key = Registry.CurrentUser.CreateSubKey(StartupKey)
            ?? throw new InvalidOperationException("Unable to access Windows startup registry key.");
        if (!enabled) { key.DeleteValue(StartupValue, false); return; }
        var executable = Environment.ProcessPath
            ?? throw new InvalidOperationException("Executable path unavailable.");
        var assemblyArgument = string.Equals(Path.GetFileNameWithoutExtension(executable), "dotnet", StringComparison.OrdinalIgnoreCase)
            ? " \"" + typeof(MainWindow).Assembly.Location + "\"" : "";
        key.SetValue(StartupValue, "\"" + executable + "\"" + assemblyArgument + " --minimized");
    }

    private void SetStartupStatus(UiMessage message) => SetUiText("SettingsStartupStatus", message);
}
