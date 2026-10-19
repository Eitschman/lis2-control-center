using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Microsoft.Win32;
using System.Diagnostics;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private bool _loadingSettings;
    private const string StartupKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValue = "LIS2 Control Center";

    private void InitializeSettings()
    {
        _loadingSettings = true;
        var mode = this.FindControl<ComboBox>("TransportModeComboBox")!;
        var port = this.FindControl<ComboBox>("SettingsPortComboBox")!;
        mode.SelectionChanged += (_, _) => port.IsEnabled = mode.SelectedIndex == 1;
        mode.SelectedIndex = 0;
        port.IsEnabled = false;
        this.FindControl<Button>("SettingsRefreshPorts")!.Click += (_, _) => RefreshSettingsPorts();
        this.FindControl<Button>("SettingsApplyTransport")!.Click += async (_, _) => await ApplySettingsTransportAsync();

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
                1 => "English", 2 => "German", 3 => "French", 4 => "Turkish", 5 => "Russian", _ => "System"
            };
            this.FindControl<TextBlock>("SettingsAppearanceStatus")!.Text =
                "Language preference saved. Full UI translation is not yet available in this port.";
            _ = SavePortableWorkspaceAsync();
        };
        this.FindControl<Border>("SettingsWindowsCard")!.IsVisible = OperatingSystem.IsWindows();
        var startup = this.FindControl<CheckBox>("StartWithWindowsCheckBox")!;
        if (OperatingSystem.IsWindows())
        {
            try { startup.IsChecked = IsWindowsStartupEnabled(); }
            catch (Exception ex) { SetStartupStatus(ex.Message); }
            startup.IsCheckedChanged += (_, _) =>
            {
                if (_loadingSettings) return;
                try
                {
                    SetWindowsStartup(startup.IsChecked == true);
                    SetStartupStatus(startup.IsChecked == true ? "Windows startup enabled." : "Windows startup disabled.");
                }
                catch (Exception ex) { SetStartupStatus("Windows startup error: " + ex.Message); }
            };
        }
        RefreshSettingsPorts();
        _loadingSettings = false;
    }

    private void LoadSettingsPreferences()
    {
        _loadingSettings = true;
        this.FindControl<ComboBox>("ThemeModeComboBox")!.SelectedIndex =
            _preferredTheme.Equals("Light", StringComparison.OrdinalIgnoreCase) ? 1 :
            _preferredTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
        this.FindControl<ComboBox>("LanguageModeComboBox")!.SelectedIndex =
            _preferredLanguage.ToLowerInvariant() switch
            {
                "english" => 1, "german" => 2, "french" => 3,
                "turkish" => 4, "russian" => 5, _ => 0
            };
        ApplyPreferredTheme();
        _loadingSettings = false;
    }

    private void ApplyPreferredTheme()
    {
        if (Application.Current is not { } app) return;
        app.RequestedThemeVariant = _preferredTheme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private void RefreshSettingsPorts()
    {
        try
        {
            var picker = this.FindControl<ComboBox>("SettingsPortComboBox")!;
            var previous = picker.SelectedItem as string;
            var ports = SerialPortDiscovery.GetAvailablePorts();
            picker.ItemsSource = ports;
            picker.SelectedItem = ports.Contains(previous) ? previous : ports.FirstOrDefault();
            this.FindControl<TextBlock>("SettingsTransportStatus")!.Text =
                ports.Count == 0 ? "No serial ports found." : $"{ports.Count} serial port(s) found.";
        }
        catch (Exception ex)
        {
            this.FindControl<TextBlock>("SettingsTransportStatus")!.Text = "Port discovery failed: " + ex.Message;
        }
    }

    private async Task ApplySettingsTransportAsync()
    {
        var status = this.FindControl<TextBlock>("SettingsTransportStatus")!;
        var serial = this.FindControl<ComboBox>("TransportModeComboBox")!.SelectedIndex == 1;
        if (!serial)
        {
            if (_serialDisplay is not null)
            {
                status.Text = "Disconnect the serial display before switching to Virtual.";
                return;
            }
            if (!_virtualDevice.IsConnected)
                await RunVirtualActionAsync(ConnectWithGlyphsAsync);
            status.Text = _virtualDevice.IsConnected ? "Virtual LIS2 connected." : "Virtual LIS2 connection failed.";
            return;
        }

        var port = this.FindControl<ComboBox>("SettingsPortComboBox")!.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(port))
        {
            status.Text = "Select a COM port first.";
            return;
        }
        if (_serialDisplay is not null)
        {
            status.Text = "Serial display is already connected. Disconnect it before reconnecting.";
            return;
        }
        var serialPicker = this.FindControl<ComboBox>("SerialPortPicker")!;
        RefreshSerialPorts();
        serialPicker.SelectedItem = port;
        if (_virtualDevice.IsConnected)
            await RunVirtualActionAsync(async () => { StopPagePlayback(); await _virtualDevice.DisconnectAsync(); });
        this.FindControl<Button>("SerialConnect")!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        status.Text = "Serial connection requested for " + port + ". See serial status below.";
    }

    private static bool IsWindowsStartupEnabled()
    {
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
        key.SetValue(StartupValue, "\"" + executable + "\" --minimized");
    }

    private void SetStartupStatus(string message) =>
        this.FindControl<TextBlock>("SettingsStartupStatus")!.Text = message;
}
