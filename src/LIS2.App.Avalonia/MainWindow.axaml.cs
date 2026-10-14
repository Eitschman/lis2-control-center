using Avalonia.Controls;
using Avalonia.Interactivity;
using LIS2.Core;
using System.Globalization;

namespace LIS2.App.Avalonia;

public partial class MainWindow : Window
{
    private readonly VirtualDeviceSession _virtualDevice = new();
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();

        for (var i = 0; i < SectionNames.Length; i++)
        {
            var index = i;
            this.FindControl<Button>($"NavButton{index}")!.Click += (_, _) => NavigateTo(index);
        }
        this.FindControl<Button>("OpenTransportSettings")!.Click += (_, _) => NavigateTo(7);

        this.FindControl<Button>("VirtualConnect")!.Click += async (_, _) =>
            await RunVirtualActionAsync(ConnectWithGlyphsAsync);
        this.FindControl<Button>("VirtualDemo")!.Click += async (_, _) =>
            await RunVirtualActionAsync(() => _virtualDevice.WriteDemoAsync());
        this.FindControl<Button>("VirtualDisconnect")!.Click += async (_, _) =>
            await RunVirtualActionAsync(async () => { StopPagePlayback(); await _virtualDevice.DisconnectAsync(); });

        this.FindControl<Button>("OpenDisplayControls")!.Click += (_, _) => NavigateTo(1);
        this.FindControl<Button>("OpenFanControl")!.Click += (_, _) => NavigateTo(6);
        this.FindControl<Button>("DashboardConnectVirtual")!.Click += async (_, _) =>
            await RunVirtualActionAsync(ConnectWithGlyphsAsync);
        this.FindControl<Button>("DashboardDisconnectVirtual")!.Click += async (_, _) =>
            await RunVirtualActionAsync(async () => { StopPagePlayback(); await _virtualDevice.DisconnectAsync(); });

        this.FindControl<Button>("SendLine1")!.Click += async (_, _) =>
            await RunVirtualActionAsync(() => _virtualDevice.WriteLineAsync(1, this.FindControl<TextBox>("Line1TextBox")!.Text ?? ""));
        this.FindControl<Button>("SendLine2")!.Click += async (_, _) =>
            await RunVirtualActionAsync(() => _virtualDevice.WriteLineAsync(2, this.FindControl<TextBox>("Line2TextBox")!.Text ?? ""));
        this.FindControl<Button>("RenderBoth")!.Click += async (_, _) =>
            await RunVirtualActionAsync(() => _virtualDevice.RenderBothAsync(
                this.FindControl<TextBox>("Line1TextBox")!.Text ?? "",
                this.FindControl<TextBox>("Line2TextBox")!.Text ?? ""));
        this.FindControl<Button>("ClearDisplay")!.Click += async (_, _) =>
            await RunVirtualActionAsync(() => _virtualDevice.ClearAsync());
        foreach (var (name, brightness) in new[]
        {
            ("Brightness100", Lis2Brightness.Percent100),
            ("Brightness75", Lis2Brightness.Percent75),
            ("Brightness50", Lis2Brightness.Percent50),
            ("Brightness25", Lis2Brightness.Percent25)
        })
        {
            this.FindControl<Button>(name)!.Click += async (_, _) =>
                await RunVirtualActionAsync(() => _virtualDevice.SetBrightnessAsync(brightness));
        }

        this.FindControl<Button>("SendSpecialCharacterTest")!.Click += async (_, _) =>
            await RunVirtualActionAsync(() => _virtualDevice.SendSpecialCharacterTestAsync());
        this.FindControl<Button>("SendCharacterRomRange")!.Click += async (_, _) =>
            await RunVirtualActionAsync(() => _virtualDevice.SendCharacterRomRangeAsync(ParseCharacterRomStart()));
        this.FindControl<Button>("CharacterRomPrevious")!.Click += (_, _) => ChangeCharacterRomRange(-0x28);
        this.FindControl<Button>("CharacterRomNext")!.Click += (_, _) => ChangeCharacterRomRange(0x28);

        this.FindControl<Button>("SendGlyphSlot")!.Click += async (_, _) =>
            await RunVirtualActionAsync(() => _virtualDevice.ProgramGlyphAsync(
                this.FindControl<ComboBox>("GlyphSlot")!.SelectedIndex + 1,
                GlyphBitmap.Parse(this.FindControl<TextBox>("GlyphBitmapEditor")!.Text)));

        InitializeSerialDisplay();
        InitializeFanEditor();
        InitializeHardwareBrowser();
        InitializeDiagnostics();
        InitializeGlyphEditor();
        InitializePagesEditor();
        this.FindControl<CheckBox>("ProgramGlyphsOnConnect")!.IsCheckedChanged += (_, _) => _ = SavePortableWorkspaceAsync();
        Closed += OnWindowClosed;
        UpdateSection();
        UpdateVirtualControls();
    }

    private async void OnWindowClosed(object? sender, EventArgs e)
    {
        StopPagePlayback();
        _hardwareRefreshTimer.Stop();
        StopHomeAssistantUi();
        await _pageTimer.StopAndDrainAsync();
        await _serialPlaybackTimer.StopAndDrainAsync();
        await _haUiTimer.StopAndDrainAsync();
        if (_virtualSequence is not null) await _virtualSequence.StopAndDrainAsync();
        await StopFanMonitorAsync();
        await StopVirtualFanSessionAsync();
        await CloseSerialDisplayAsync();
        if (_liveFanSensors is IDisposable fanSensors) fanSensors.Dispose();
        await _virtualDevice.DisposeAsync();
        await _runtime.DisposeAsync();
    }

    private async Task RunVirtualActionAsync(Func<Task> action)
    {
        if (_busy || _virtualSequence?.IsBusy == true)
            return;

        // Manual display actions take ownership of the virtual device.
        StopPagePlayback();

        _busy = true;
        UpdateVirtualControls();
        try
        {
            await action();
            UpdatePreview();
            SetVirtualStatus(_virtualDevice.IsConnected
                ? $"Virtual LIS2: connected ({_virtualDevice.CommandCount} commands)"
                : "Virtual LIS2: disconnected");
        }
        catch (Exception ex)
        {
            SetVirtualStatus($"Virtual LIS2 error: {ex.Message}");
        }
        finally
        {
            _busy = false;
            UpdateVirtualControls();
        }
    }

    private int ParseCharacterRomStart()
    {
        var text = this.FindControl<TextBox>("CharacterRomStartTextBox")!.Text?.Trim();
        if (!int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var start)
            || start is < 0x20 or > 0xD8)
            throw new InvalidOperationException("ROM start byte must be hexadecimal 20 through D8.");

        SetCharacterRomStart(start);
        return start;
    }

    private void SetCharacterRomStart(int start)
    {
        start = Math.Clamp(start, 0x20, 0xD8);
        this.FindControl<TextBox>("CharacterRomStartTextBox")!.Text = start.ToString("X2", CultureInfo.InvariantCulture);
        this.FindControl<TextBlock>("CharacterRomRangeText")!.Text = $"{start:X2}-{start + 39:X2}";
    }

    private void ChangeCharacterRomRange(int offset)
    {
        try
        {
            SetCharacterRomStart(ParseCharacterRomStart() + offset);
        }
        catch (Exception ex)
        {
            SetVirtualStatus($"Virtual LIS2 error: {ex.Message}");
        }
    }

    private void UpdatePreview()
    {
        var first = this.FindControl<TextBlock>("PreviewLine1");
        var second = this.FindControl<TextBlock>("PreviewLine2");
        if (first is null || second is null)
            return;

        first.Text = _virtualDevice.IsConnected ? _virtualDevice.State.Line1 : new string(' ', 20);
        second.Text = _virtualDevice.IsConnected ? _virtualDevice.State.Line2 : new string(' ', 20);
    }

    private void SetVirtualStatus(string message)
    {
        var status = this.FindControl<TextBlock>("VirtualStatus");
        if (status is not null)
            status.Text = message;
    }

    private void UpdateVirtualControls()
    {
        var connected = _virtualDevice.IsConnected;
        var playbackBusy = _virtualSequence?.IsBusy == true;
        var playbackRunning = _virtualSequence?.IsRunning == true;
        this.FindControl<Button>("VirtualConnect")!.IsEnabled = !_busy && !playbackBusy && !connected;
        this.FindControl<Button>("VirtualDemo")!.IsEnabled = !_busy && !playbackBusy && !playbackRunning && connected;
        this.FindControl<Button>("VirtualDisconnect")!.IsEnabled = !_busy && !playbackBusy && connected;
        this.FindControl<Button>("DashboardConnectVirtual")!.IsEnabled = !_busy && !playbackBusy && !connected;
        this.FindControl<Button>("DashboardDisconnectVirtual")!.IsEnabled = !_busy && !playbackBusy && connected;
        foreach (var name in new[] { "SendLine1", "SendLine2", "RenderBoth", "ClearDisplay",
                                    "Brightness100", "Brightness75", "Brightness50", "Brightness25",
                                    "SendSpecialCharacterTest", "SendCharacterRomRange", "SendGlyphSlot", "SendAllGlyphs" })
            this.FindControl<Button>(name)!.IsEnabled = !_busy && !playbackBusy && !playbackRunning && connected;
        var badge = this.FindControl<TextBlock>("ConnectionText");
        if (badge is not null)
            badge.Text = DashboardConnectionState.Badge(connected);
        var virtualState = this.FindControl<TextBlock>("VirtualStateText");
        if (virtualState is not null)
            virtualState.Text = DashboardConnectionState.Description(connected, _virtualDevice.CommandCount);
    }

    private static readonly string[] SectionNames = { "Dashboard", "Display", "Pages", "Media", "Events", "Hardware", "Fan Control", "Settings", "Diagnostics", "Home Assistant" };
    private int _selectedSection;

    private void NavigateTo(int index)
    {
        if (index < 0 || index >= SectionNames.Length) return;
        _selectedSection = index;
        UpdateSection();
    }

    private void UpdateSection()
    {
        var section = SectionNames[_selectedSection];
        for (var i = 0; i < SectionNames.Length; i++)
        {
            var button = this.FindControl<Button>($"NavButton{i}")!;
            button.Background = i == _selectedSection ? new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#354351")) : global::Avalonia.Media.Brushes.Transparent;
            button.Foreground = global::Avalonia.Media.Brushes.White;
            button.FontWeight = i == _selectedSection ? global::Avalonia.Media.FontWeight.SemiBold : global::Avalonia.Media.FontWeight.Normal;
        }
        var title = this.FindControl<TextBlock>("SectionTitle");
        var description = this.FindControl<TextBlock>("SectionDescription");

        if (title is not null)
            title.Text = section;

        this.FindControl<ScrollViewer>("DashboardView")!.IsVisible = section == "Dashboard";
        this.FindControl<ScrollViewer>("DisplayView")!.IsVisible = section == "Display";
        this.FindControl<ScrollViewer>("PagesView")!.IsVisible = section == "Pages";
        this.FindControl<ScrollViewer>("FanControlView")!.IsVisible = section == "Fan Control";
        this.FindControl<ScrollViewer>("SettingsView")!.IsVisible = section == "Settings";
        this.FindControl<ScrollViewer>("HardwareView")!.IsVisible = section == "Hardware";
        this.FindControl<ScrollViewer>("DiagnosticsView")!.IsVisible = section == "Diagnostics";
        if (section == "Diagnostics") RefreshDiagnostics();

        if (description is not null)
            description.Text = section == "Dashboard"
                ? "Overview and quick access to the most important functions."
                : section == "Display"
                    ? "Quick display tests, custom characters and brightness."
                : section == "Pages"
                    ? "Manage display pages, templates, conditions and live preview."
                    : section == "Hardware"
                    ? "Browse platform temperature sensors and assign a sensor to a fan channel."
                    : section == "Settings"
                    ? "Configure transport and inspect experimental connection controls."
                    : section == "Fan Control"
                    ? "Configure four fan channels and test the regulation in a safe simulation."
                    : $"{section} is not connected to the LIS2 runtime yet.";
    }
}
