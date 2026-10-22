using Avalonia.Controls;
using Avalonia.Interactivity;
using LIS2.Core;
using LIS2.Runtime;
using System.Globalization;

namespace LIS2.App.Avalonia;

public partial class MainWindow : Window
{
    private readonly VirtualDeviceSession _virtualDevice = new();
    private bool _busy;
    private readonly bool _startBackgroundSources;
    private readonly Func<string, SerialDisplaySession> _serialSessionFactory;
    private readonly DiagnosticJournal _applicationJournal = new();
    private readonly List<DiagnosticJournal> _serialJournals = new();
    private Task _sourceStartup = Task.CompletedTask;
    private Task _transportStartup = Task.CompletedTask;
    private bool _initialOpenHandled;
    internal bool CloseToTray { get; set; }

    public MainWindow() : this(new PortableWorkspaceStore(), new PortableHaTokenStore(), true,
        port => new SerialDisplaySession(port)) { }

    /// <summary>Erlaubt isolierte UI-Prüfungen mit eigenem Workspace und simuliertem Transport.</summary>
    internal MainWindow(PortableWorkspaceStore workspaceStore, PortableHaTokenStore tokenStore,
        bool startBackgroundSources, Func<string, SerialDisplaySession> serialSessionFactory,
        PageSourceValues? pageSources = null, IFanSensorSource? fanSensors = null)
    {
        _runtime = new DisplayRuntimeHost(pageSources);
        _workspaceStore = workspaceStore;
        _haTokenStore = tokenStore;
        _startBackgroundSources = startBackgroundSources;
        _serialSessionFactory = serialSessionFactory;
        _liveFanSensors = fanSensors ?? (!startBackgroundSources ? new SimulatedFanSensors() :
            OperatingSystem.IsLinux() ? new LinuxSystemSensors() :
            OperatingSystem.IsWindows() ? new WindowsHardwareFanSensors() :
            new SnapshotFanSensors(() => new Dictionary<string, object?>()));
        InitializeComponent();
        SizeChanged += (_, _) => UpdatePageEditorLayout();

        for (var i = 0; i < SectionNames.Length; i++)
        {
            var index = i;
            this.FindControl<Button>($"NavButton{index}")!.Click += (_, _) => NavigateTo(index);
        }
        this.FindControl<Button>("OpenTransportSettings")!.Click += (_, _) => NavigateTo(7);
        this.FindControl<Button>("OpenHelp")!.Click += async (_, _) => await OpenHelpAsync();
        KeyDown += async (_, args) =>
        {
            if (args.Key != global::Avalonia.Input.Key.F1) return;
            args.Handled = true;
            await OpenHelpAsync();
        };
        this.FindControl<Button>("OpenAbout")!.Click += async (_, _) => await OpenAboutAsync();

        this.FindControl<Button>("VirtualConnect")!.Click += async (_, _) =>
            await RunVirtualActionAsync(ConnectWithGlyphsAsync);
        this.FindControl<Button>("VirtualDemo")!.Click += async (_, _) =>
            await RunSelectedDisplayActionAsync(session => session.RenderBothAsync("LIS2 Control Center", "Windows / Linux"));
        this.FindControl<Button>("VirtualDisconnect")!.Click += async (_, _) =>
            await RunVirtualActionAsync(async () => { StopPagePlayback(); await _virtualDevice.DisconnectAsync(); });

        this.FindControl<Button>("OpenDisplayControls")!.Click += (_, _) => NavigateTo(1);
        this.FindControl<Button>("OpenFanControl")!.Click += (_, _) => NavigateTo(6);

        this.FindControl<Button>("SendLine1")!.Click += async (_, _) =>
            await RunSelectedDisplayActionAsync(session => session.WriteLineAsync(1, this.FindControl<TextBox>("Line1TextBox")!.Text ?? ""));
        this.FindControl<Button>("SendLine2")!.Click += async (_, _) =>
            await RunSelectedDisplayActionAsync(session => session.WriteLineAsync(2, this.FindControl<TextBox>("Line2TextBox")!.Text ?? ""));
        this.FindControl<Button>("RenderBoth")!.Click += async (_, _) =>
            await RunSelectedDisplayActionAsync(session => session.RenderBothAsync(
                this.FindControl<TextBox>("Line1TextBox")!.Text ?? "",
                this.FindControl<TextBox>("Line2TextBox")!.Text ?? ""));
        this.FindControl<Button>("ClearDisplay")!.Click += async (_, _) =>
            await RunSelectedDisplayActionAsync(session => session.ClearAsync());
        foreach (var (name, percent) in new[]
        {
            ("Brightness100", 100), ("Brightness75", 75),
            ("Brightness50", 50), ("Brightness25", 25)
        })
        {
            this.FindControl<Button>(name)!.Click += async (_, _) =>
                await SetSelectedBrightnessAsync(percent);
        }

        this.FindControl<Button>("SendSpecialCharacterTest")!.Click += async (_, _) =>
            await RunSelectedDisplayActionAsync(session => session.RenderBothAsync("ÄÖÜ äöü ß ° µ ± £", "← → Ω π Σ √ ∞ × ÷"));
        this.FindControl<Button>("SendCharacterRomRange")!.Click += async (_, _) =>
            await SendSelectedCharacterRomAsync(ParseCharacterRomStart());
        this.FindControl<Button>("CharacterRomPrevious")!.Click += (_, _) => ChangeCharacterRomRange(-0x28);
        this.FindControl<Button>("CharacterRomNext")!.Click += (_, _) => ChangeCharacterRomRange(0x28);

        this.FindControl<Button>("SendGlyphSlot")!.Click += async (_, _) =>
            await RunSelectedDisplayActionAsync(session => session.ProgramGlyphAsync(
                this.FindControl<ComboBox>("GlyphSlot")!.SelectedIndex + 1,
                GlyphBitmap.Parse(this.FindControl<TextBox>("GlyphBitmapEditor")!.Text)));

        InitializeSerialDisplay();
        InitializeDashboardFans();
        InitializeFanEditor();
        InitializeHardwareBrowser();
        InitializeDiagnostics();
        InitializeGlyphEditor();
        InitializePagesEditor();
        InitializeMedia();
        InitializeSettings();
        InitializeEvents();
        this.FindControl<CheckBox>("ProgramGlyphsOnConnect")!.IsCheckedChanged += (_, _) => _ = SavePortableWorkspaceAsync();
        Closing += OnWindowClosing;
        // The virtual transport is hardware-free: make the WPF-style preview
        // immediately testable without requiring an experimental serial connection.
        Opened += async (_, _) =>
        {
            // Wiederherstellen aus dem Tray darf keine Verbindung oder Runtime neu starten.
            if (_initialOpenHandled) return;
            _initialOpenHandled = true;
            // The visual tree is not fully attached in the constructor; translate
            // page controls after Opened so system-language defaults reach them.
            ApplyPageLocalization();
            // Respect the selected transport: opening the window must never
            // connect the virtual simulator while serial mode is selected.
            // WPF starts page rendering automatically after transport connection.
            _transportStartup = ApplySettingsTransportAsync();
            await _transportStartup;
        };
        UpdateSection();
        UpdateVirtualControls();
    }

    internal Task ShutdownCompletion { get; private set; } = Task.CompletedTask;
    private bool _windowClosed;

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_shutdownFinished) return;
        e.Cancel = true;
        if (CloseToTray && !_windowClosed) { Hide(); return; }
        try { await BeginShutdownAsync(); }
        catch (Exception exception)
        {
            _applicationJournal.Record("Shutdown", "ERROR " + exception.GetType().Name);
            System.Diagnostics.Trace.TraceError("Shutdown failed: {0}", exception);
        }
        finally { _shutdownFinished = true; Close(); }
    }

    private bool _shutdownFinished;

    /// <summary>Der Desktop darf erst nach Drain und Sicherheitswiederherstellung beendet werden.</summary>
    internal Task BeginShutdownAsync()
    {
        if (_windowClosed) return ShutdownCompletion;
        _windowClosed = true;
        IsEnabled = false;
        _applicationJournal.Record("Application", "Shutdown started");
        ShutdownCompletion = ShutdownRuntimeAsync();
        return ShutdownCompletion;
    }

    private async Task ShutdownRuntimeAsync()
    {
        _editorPreviewTimer.Stop();
        StopPagePlayback();
        StopSerialPlayback();
        _eventTimer.Stop();
        _hardwareRefreshTimer.Stop();
        StopHomeAssistantUi();
        await RuntimeShutdown.RunAsync(
            () => _transportStartup, () => _sourceStartup,
            DrainDeviceActionsAsync,
            StopMediaAsync,
            () => _pageTimer.StopAndDrainAsync(), () => _serialPlaybackTimer.StopAndDrainAsync(),
            () => _haUiTimer.StopAndDrainAsync(),
            () => _virtualSequence?.StopAndDrainAsync() ?? Task.CompletedTask,
            StopFanMonitorAsync, StopVirtualFanSessionAsync, CloseSerialDisplayAsync,
            () => { if (_liveFanSensors is IDisposable sensors) sensors.Dispose(); return Task.CompletedTask; },
            () => _virtualDevice.DisposeAsync().AsTask(), () => _runtime.DisposeAsync().AsTask());
        _applicationJournal.Record("Application", "Shutdown drained");
    }

    private async Task DrainDeviceActionsAsync()
    {
        while (_busy || _serialBusy || _settingsTransportBusy || _dashboardFansBusy || _haActionBusy)
            await Task.Delay(10);
        // Ein bereits laufender Connect darf während seines Abschlusses Timer gestartet haben.
        StopPagePlayback();
        _eventTimer.Stop();
        _hardwareRefreshTimer.Stop();
        StopHomeAssistantUi();
    }

    private async Task RunVirtualActionAsync(Func<Task> action)
    {
        if (_busy || _virtualSequence?.IsBusy == true)
            return;

        // Reserve manual ownership before awaiting any drain. A scheduled
        // playback callback must finish before a manual command can be sent.
        _busy = true;
        StopPagePlayback();
        UpdateVirtualControls();
        try
        {
            await _pageTimer.StopAndDrainAsync();
            if (_virtualSequence is not null)
                await _virtualSequence.StopAndDrainAsync();
            await action();
            UpdatePreview();
            SetVirtualStatus(_virtualDevice.IsConnected
                ? (UiMessage)$"Virtual LIS2: connected ({_virtualDevice.CommandCount} commands)"
                : "Virtual LIS2: disconnected");
        }
        catch (Exception ex)
        {
            SetVirtualStatus($"Virtual LIS2 error: {(UiMessage)ex.Message}");
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
            SetVirtualStatus($"Virtual LIS2 error: {(UiMessage)ex.Message}");
        }
    }

    private void UpdatePreview()
    {
        var first = this.FindControl<TextBlock>("PreviewLine1");
        var second = this.FindControl<TextBlock>("PreviewLine2");
        if (first is null || second is null)
            return;

        first.Text = LIS2.Display.GlyphRendering.Preview(SerialSelected ? _serialDisplay?.LastLine1 ?? new string(' ', 20)
            : _virtualDevice.IsConnected ? _virtualDevice.State.Line1 : new string(' ', 20), _glyphs.IsSpectrumSet);
        second.Text = LIS2.Display.GlyphRendering.Preview(SerialSelected ? _serialDisplay?.LastLine2 ?? new string(' ', 20)
            : _virtualDevice.IsConnected ? _virtualDevice.State.Line2 : new string(' ', 20), _glyphs.IsSpectrumSet);
        RefreshDiagnostics();
    }

    private void SetVirtualStatus(UiMessage message)
    {
        _applicationJournal.Record("Application", message.Render("en"));
        var status = this.FindControl<TextBlock>("VirtualStatus");
        if (status is not null)
            SetUiText(status, message);
        SetUiText("DeviceActionStatus", message);
    }

    private void UpdateVirtualControls()
    {
        var connected = SelectedDisplay?.IsConnected == true;
        var playbackBusy = (SerialSelected ? _serialSequence : _virtualSequence)?.IsBusy == true;
        this.FindControl<Button>("VirtualConnect")!.IsEnabled = !_busy && !playbackBusy && !_virtualDevice.IsConnected;
        this.FindControl<Button>("VirtualDemo")!.IsEnabled = !_busy && !_serialBusy && !_settingsTransportBusy && !playbackBusy && connected;
        this.FindControl<Button>("VirtualDisconnect")!.IsEnabled = !_busy && !playbackBusy && _virtualDevice.IsConnected;
        foreach (var name in new[] { "SendLine1", "SendLine2", "RenderBoth", "ClearDisplay",
                                    "Brightness100", "Brightness75", "Brightness50", "Brightness25",
                                    "SendSpecialCharacterTest", "SendCharacterRomRange", "SendGlyphSlot", "SendAllGlyphs" })
            this.FindControl<Button>(name)!.IsEnabled = !_busy && !_serialBusy && !_settingsTransportBusy && !playbackBusy && connected;
        var badge = this.FindControl<TextBlock>("ConnectionText");
        if (badge is not null)
            SetUiText(badge, connected ? SerialSelected ? "Connected (Serial)" : "Connected (Virtual)" : "Disconnected");
        SetUiText("TransportSummaryText", SerialSelected ? (UiMessage)$"Serial: {_preferredPortName}" : "Virtual LIS2");
        this.FindControl<Button>("SettingsApplyTransport")!.IsEnabled = !_settingsTransportBusy && !_serialBusy && !_busy && !_dashboardFansBusy;
        this.FindControl<Button>("ImportWindowsSettings")!.IsEnabled = !_settingsTransportBusy && !_serialBusy && !_busy && !_dashboardFansBusy;
        this.FindControl<Button>("SettingsDisconnectTransport")!.IsEnabled = connected && !_settingsTransportBusy && !_serialBusy && !_busy && !_dashboardFansBusy;
        this.FindControl<ComboBox>("TransportModeComboBox")!.IsEnabled = !_settingsTransportBusy;
        this.FindControl<ComboBox>("SettingsPortComboBox")!.IsEnabled = !_settingsTransportBusy && this.FindControl<ComboBox>("TransportModeComboBox")!.SelectedIndex == 1;
        this.FindControl<Button>("PageTest")!.IsEnabled = connected && !_settingsTransportBusy && !_serialBusy && !_busy;
        UpdatePagePlaybackControls();
        UpdateDashboardFanControls();
        var virtualState = this.FindControl<TextBlock>("VirtualStateText");
        if (virtualState is not null)
            SetUiText(virtualState, SerialSelected
                ? connected ? (UiMessage)$"LIS2 connected: {_preferredPortName}" : (UiMessage)$"LIS2 disconnected: {_preferredPortName}"
                : _virtualDevice.IsConnected
                    ? (UiMessage)$"Virtual LIS2 connected. Commands: {_virtualDevice.CommandCount}."
                    : "State available when Virtual transport is active.");
    }

    private static readonly string[] SectionNames = { "Dashboard", "Display", "Pages", "Media", "Events", "Hardware", "Fan Control", "Settings", "Diagnostics", "Home Assistant" };

    /// <summary>Bei schmalen Fenstern bleiben Seitenliste und Editor untereinander zugänglich.</summary>
    private void UpdatePageEditorLayout()
    {
        var stacked = Bounds.Width < 1200;
        var layout = this.FindControl<Grid>("PageEditorLayout")!;
        var editor = this.FindControl<StackPanel>("PagePropertiesPane")!;
        layout.ColumnDefinitions = new ColumnDefinitions(stacked ? "*" : "250,18,*");
        layout.RowDefinitions = new RowDefinitions(stacked ? "Auto,Auto" : "Auto");
        Grid.SetColumn(editor, stacked ? 0 : 2);
        Grid.SetRow(editor, stacked ? 1 : 0);
        editor.Margin = new global::Avalonia.Thickness(0, stacked ? 18 : 0, 0, 0);
    }
    private int _selectedSection;

    private void NavigateTo(int index)
    {
        if (index < 0 || index >= SectionNames.Length) return;
        _selectedSection = index;
        UpdateSection();
        UpdateEditorPreviewVisibility();
        // Some pages populate their controls only when selected. Localize those
        // newly realized controls without altering live/bound telemetry values.
        ApplyPageLocalization();
    }

    private void UpdateSection()
    {
        var section = SectionNames[_selectedSection];
        for (var i = 0; i < SectionNames.Length; i++)
        {
            var button = this.FindControl<Button>($"NavButton{i}")!;
            var dark = global::Avalonia.Application.Current?.ActualThemeVariant == global::Avalonia.Styling.ThemeVariant.Dark;
            button.Background = i == _selectedSection
                ? new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse(dark ? "#26583E" : "#DDF6E7"))
                : global::Avalonia.Media.Brushes.Transparent;
            button.Foreground = new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse(dark ? "#EDF3FA" : "#10243A"));
            button.FontWeight = i == _selectedSection ? global::Avalonia.Media.FontWeight.SemiBold : global::Avalonia.Media.FontWeight.Normal;
        }
        var title = this.FindControl<TextBlock>("SectionTitle");

        if (title is not null)
            title.Text = SettingsTranslate(section);

        this.FindControl<ScrollViewer>("DashboardView")!.IsVisible = section == "Dashboard";
        this.FindControl<ScrollViewer>("DisplayView")!.IsVisible = section == "Display";
        this.FindControl<ScrollViewer>("PagesView")!.IsVisible = section == "Pages";
        this.FindControl<ScrollViewer>("MediaView")!.IsVisible = section == "Media";
        this.FindControl<ScrollViewer>("HomeAssistantView")!.IsVisible = section == "Home Assistant";
        this.FindControl<ScrollViewer>("FanControlView")!.IsVisible = section == "Fan Control";
        this.FindControl<ScrollViewer>("SettingsView")!.IsVisible = section == "Settings";
        this.FindControl<ScrollViewer>("EventsView")!.IsVisible = section == "Events";
        this.FindControl<ScrollViewer>("HardwareView")!.IsVisible = section == "Hardware";
        this.FindControl<ScrollViewer>("DiagnosticsView")!.IsVisible = section == "Diagnostics";
        if (section == "Diagnostics") RefreshDiagnostics();
        if (section == "Media")
        {
            // A provider failure must never escape the navigation click handler.
            try { RefreshMedia(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Media navigation failed: {0}", ex);
                var status = this.FindControl<TextBlock>("WindowsMediaStatus");
                if (status is not null) SetUiText(status, $"Provider error: {ex.GetBaseException().Message}");
            }
        }


    }
}
