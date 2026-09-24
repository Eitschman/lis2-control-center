using System.Globalization;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LIS2.Core;
using LIS2.Display;
using LIS2.Fans;
using LIS2.Sources;
using LIS2.Winamp;

namespace LIS2.App;

public partial class MainWindow : Window
{
    private readonly SettingsStore _settingsStore = new();
    private readonly DataSourceRegistry _sources = new();
    private readonly PageScheduler _pageScheduler = new();
    private readonly EventQueue _eventQueue = new();
    private readonly DisplayRuntime _displayRuntime;
    private readonly DispatcherTimer _pageTimer;
    private readonly DispatcherTimer _fanTimer;
    private readonly DispatcherTimer _hardwareTimer;
    private readonly DispatcherTimer _winampTimer;
    private readonly DispatcherTimer _eventUiTimer;
    private readonly WinampDataSource _winampSource = new();
    private readonly FanController _fanController = new();
    private int[]? _lastAutomaticFanOutputs;
    private readonly double?[] _lastAutomaticFanSensorValues = new double?[4];
    private readonly double?[] _currentFanSensorValues = new double?[4];
    private CustomCharacterManager? _customCharacterManager;
    private readonly TrayIconService _trayIcon = new();
    private readonly StartupService _startupService = new();
    private bool _allowClose;
    private bool _loadingStartupSetting;

    private AppSettings _settings = new();
    private ILis2Transport? _transport;
    private Lis2Device? _device;
    private DisplayFrameWriter? _frameWriter;
    private DisplayFrame _frame = DisplayFrame.Create(string.Empty, string.Empty);

    public MainWindow()
    {
        InitializeComponent();
        LocalizationService.ApplyTo(this);

        _displayRuntime = new DisplayRuntime(
            new TemplateRenderer(),
            _pageScheduler,
            _eventQueue);

        _pageTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _pageTimer.Tick += PageTimer_Tick;

        _fanTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _fanTimer.Tick += FanTimer_Tick;

        _hardwareTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _hardwareTimer.Tick += HardwareTimer_Tick;

        _winampTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _winampTimer.Tick += WinampTimer_Tick;

        _eventUiTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _eventUiTimer.Tick += EventUiTimer_Tick;

        _eventQueue.Changed += EventQueue_Changed;
        _winampSource.Changed += WinampSource_Changed;

        _sources.Add(new ClockDataSource());
        _sources.Add(_winampSource);
        _sources.Add(new LibreHardwareMonitorDataSource());

        _trayIcon.ShowRequested += TrayIcon_ShowRequested;
        _trayIcon.ExitRequested += TrayIcon_ExitRequested;

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;
    }

    private static readonly (string Title, string Subtitle)[] Sections =
    [
        ("Dashboard", "Overview and quick access to the most important functions."),
        ("Display", "VFD output, brightness and direct display tests."),
        ("Pages", "Create and edit the rotating 20x2 display pages."),
        ("Winamp", "Winamp integration, pipe transport and available media variables."),
        ("Events", "Priority notifications and temporary VFD overlays."),
        ("Hardware", "LibreHardwareMonitor data sources and sensor availability."),
        ("Fan Control", "Manual output, automatic control, curves and safety limits."),
        ("Settings", "LIS2 transport, COM port and Windows startup behavior."),
        ("Diagnostics", "Runtime state, data-source health and protocol traffic.")
    ];

    private void Navigation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string tag } ||
            !int.TryParse(tag, out var index) ||
            index < 0 ||
            index >= MainTabs.Items.Count)
        {
            return;
        }

        MainTabs.SelectedIndex = index;
        UpdateNavigationSelection(index);
    }

    private void UpdateNavigationSelection(int selectedIndex)
    {
        foreach (var button in NavigationPanel.Children.OfType<System.Windows.Controls.Button>())
        {
            var isSelected =
                button.Tag is string tag &&
                int.TryParse(tag, out var index) &&
                index == selectedIndex;

            if (isSelected)
            {
                button.SetResourceReference(
                    System.Windows.Controls.Control.BackgroundProperty,
                    "AccentDarkBrush");
                button.SetResourceReference(
                    System.Windows.Controls.Control.ForegroundProperty,
                    "AccentBrush");
            }
            else
            {
                button.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
                button.ClearValue(System.Windows.Controls.Control.ForegroundProperty);
            }
        }
    }

    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, MainTabs))
            return;

        var index = MainTabs.SelectedIndex;

        if (index < 0 || index >= Sections.Length)
            return;

        SectionTitleText.Text = LocalizationService.Translate(Sections[index].Title);
        SectionSubtitleText.Text = LocalizationService.Translate(Sections[index].Subtitle);
        UpdateNavigationSelection(index);

        if (index == 3)
            RefreshWinampView();

        if (index == 4)
            RefreshEventsView();

        if (index == 5)
            RefreshHardwareSensors();

        if (index == 6)
        {
            RefreshFanSensorChoices();
            RefreshFanLiveStatus();
        }

        if (index == 8)
            RefreshDiagnostics();
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings = await _settingsStore.LoadAsync();
            _settings.EnsureDefaults();

            RefreshPorts();
            ApplySettingsToUi();
            AutomaticFanControlCheckBox.IsChecked = _settings.Fans.AutomaticControlEnabled;

            _loadingStartupSetting = true;
            StartWithWindowsCheckBox.IsChecked = _startupService.IsEnabled();
            _loadingStartupSetting = false;

            LoadPagesIntoRuntime();
            BindPages();
            BindCustomGlyphs();
            BindFanChannels();

            await ReconnectAsync();
            await _sources.StartAllAsync();
            LogSourceHealth();
            RefreshWinampView();
            RefreshHardwareSensors();
            RefreshFanSensorChoices();
            await RenderRuntimePageAsync();

            _pageTimer.Start();
            _fanTimer.Start();
            _hardwareTimer.Start();
            _winampTimer.Start();
            _eventUiTimer.Start();

            RefreshEventsView();
            RefreshDiagnostics();

            if (Environment.GetCommandLineArgs().Any(
                    arg => string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase)))
            {
                Hide();
                _trayIcon.SetStatus("running in tray");
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void MainWindow_Closing(
        object? sender,
        System.ComponentModel.CancelEventArgs e)
    {
        if (_allowClose)
            return;

        e.Cancel = true;
        Hide();
        _trayIcon.SetStatus("running in tray");
    }

    private void TrayIcon_ShowRequested(object? sender, EventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void TrayIcon_ExitRequested(object? sender, EventArgs e)
    {
        _allowClose = true;
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    private async void MainWindow_Closed(object? sender, EventArgs e)
    {
        _pageTimer.Stop();
        _fanTimer.Stop();
        _hardwareTimer.Stop();
        _winampTimer.Stop();
        _eventUiTimer.Stop();
        _eventQueue.Changed -= EventQueue_Changed;
        _winampSource.Changed -= WinampSource_Changed;

        await _sources.StopAllAsync();
        await _sources.DisposeAsync();

        if (_device is not null)
            await _device.DisposeAsync();

        _trayIcon.Dispose();
    }

    private async void FanTimer_Tick(object? sender, EventArgs e)
    {
        if (!_settings.Fans.AutomaticControlEnabled)
            return;

        try
        {
            await ApplyAutomaticFanControlAsync();
        }
        catch (Exception ex)
        {
            Log($"ERR  automatic fan control: {ex.Message}");
        }
    }

    private async Task ApplyAutomaticFanControlAsync()
    {
        if (_device?.IsConnected != true)
            return;

        var snapshot = _sources.Snapshot();
        var outputs = new int[4];

        for (var index = 0; index < 4; index++)
        {
            var stored = _settings.Fans.Channels[index];
            var mode = Enum.TryParse<FanMode>(
                stored.Mode,
                ignoreCase: true,
                out var parsedMode)
                ? parsedMode
                : FanMode.Fixed;

            var configuration = new FanChannelConfiguration
            {
                Mode = mode,
                SensorKey = stored.SensorKey,
                FixedPercent = stored.FixedPercent,
                MinimumPercent = stored.MinimumPercent,
                MaximumPercent = stored.MaximumPercent,
                FailSafePercent = stored.FailSafePercent,
                HysteresisDegrees = stored.HysteresisDegrees,
                AllowStop = stored.AllowStop,
                Curve = stored.Curve
                    .Select(point => new FanCurvePoint(
                        point.Temperature,
                        point.OutputPercent))
                    .ToList()
            };

            double? sensorValue = null;
            var sensorValid = true;

            if (mode is FanMode.Curve or FanMode.Follow)
            {
                sensorValid =
                    !string.IsNullOrWhiteSpace(stored.SensorKey) &&
                    snapshot.TryGetValue(stored.SensorKey, out var rawValue) &&
                    TryConvertToDouble(rawValue, out sensorValue);
            }

            _currentFanSensorValues[index] = sensorValid ? sensorValue : null;

            outputs[index] = _fanController.CalculateOutput(
                configuration,
                sensorValue,
                externalPercent: null,
                sensorValid,
                previousSensorValue: _lastAutomaticFanSensorValues[index],
                previousOutputPercent: _lastAutomaticFanOutputs?[index]);
        }

        var outputsChanged =
            _lastAutomaticFanOutputs is null ||
            !outputs.SequenceEqual(_lastAutomaticFanOutputs);

        for (var index = 0; index < _lastAutomaticFanSensorValues.Length; index++)
            _lastAutomaticFanSensorValues[index] = _currentFanSensorValues[index];

        if (outputsChanged)
        {
            await RequireDevice().SetFansAsync(
                outputs[0],
                outputs[1],
                outputs[2],
                outputs[3]);

            _lastAutomaticFanOutputs = outputs;
            UpdateFanOutputFields(outputs);
            Log($"INFO automatic fan outputs: {string.Join("/", outputs)}%");
        }

        RefreshFanLiveStatus();
    }

    private static bool TryConvertToDouble(object? value, out double? result)
    {
        result = null;

        if (value is null)
            return false;

        try
        {
            result = Convert.ToDouble(
                value,
                CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (
            ex is FormatException or InvalidCastException or OverflowException)
        {
            return false;
        }
    }

    private async void AutomaticFanControlChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        _settings.Fans.AutomaticControlEnabled =
            AutomaticFanControlCheckBox.IsChecked == true;

        _lastAutomaticFanOutputs = null;
        Array.Clear(_lastAutomaticFanSensorValues);
        await _settingsStore.SaveAsync(_settings);

        Log(_settings.Fans.AutomaticControlEnabled
            ? "INFO automatic fan control enabled"
            : "INFO automatic fan control disabled");

        if (_settings.Fans.AutomaticControlEnabled)
            await ApplyAutomaticFanControlAsync();
    }

    private void BindFanChannels()
    {
        FanChannelsListBox.DisplayMemberPath = nameof(FanChannelSettings.DisplayName);
        FanChannelsListBox.ItemsSource = null;
        FanChannelsListBox.ItemsSource = _settings.Fans.Channels;

        if (FanChannelsListBox.SelectedIndex < 0)
            FanChannelsListBox.SelectedIndex = 0;
    }

    private void FanChannelsListBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (FanChannelsListBox.SelectedItem is not FanChannelSettings channel)
            return;

        FanNameTextBox.Text = channel.Name;
        SelectFanMode(channel.Mode);
        FanSensorComboBox.Text = channel.SensorKey ?? string.Empty;
        FanFixedTextBox.Text = channel.FixedPercent.ToString(CultureInfo.InvariantCulture);
        FanMinimumTextBox.Text = channel.MinimumPercent.ToString(CultureInfo.InvariantCulture);
        FanMaximumTextBox.Text = channel.MaximumPercent.ToString(CultureInfo.InvariantCulture);
        FanFailSafeTextBox.Text = channel.FailSafePercent.ToString(CultureInfo.InvariantCulture);
        FanHysteresisTextBox.Text = channel.HysteresisDegrees.ToString("0.##", CultureInfo.InvariantCulture);
        FanAllowStopCheckBox.IsChecked = channel.AllowStop;
        FanCurveTextBox.Text = FormatFanCurve(channel.Curve);
        RefreshFanCurvePreview(channel.Curve);
        RefreshFanLiveStatus();
    }

    private void SelectFanMode(string mode)
    {
        foreach (var item in FanModeComboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(
                    Convert.ToString(item.Tag, CultureInfo.InvariantCulture),
                    mode,
                    StringComparison.OrdinalIgnoreCase))
            {
                FanModeComboBox.SelectedItem = item;
                return;
            }
        }

        FanModeComboBox.SelectedIndex = 0;
    }

    private string GetSelectedFanMode() =>
        FanModeComboBox.SelectedItem is ComboBoxItem { Tag: string tag }
            ? tag
            : nameof(FanMode.Fixed);

    private void RefreshFanSensors_Click(object sender, RoutedEventArgs e) =>
        RefreshFanSensorChoices();

    private void WinampSource_Changed(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(RefreshWinampView);
    }

    private void WinampTimer_Tick(object? sender, EventArgs e)
    {
        if (MainTabs.SelectedIndex == 3)
            RefreshWinampView();
    }

    private void RefreshWinampView()
    {
        var values = _winampSource.Values;

        var state = GetWinampValue(values, "State") ?? "Unknown";
        var localizedState = LocalizationService.Translate(state);
        var artist = GetWinampValue(values, "Artist");
        var title = GetWinampValue(values, "Title");
        var album = GetWinampValue(values, "Album");
        var elapsed = GetWinampValue(values, "Elapsed") ?? "--:--";
        var duration = GetWinampValue(values, "Duration") ?? "--:--";
        var playlistPosition = GetWinampValue(values, "PlaylistPosition") ?? "-";
        var playlistCount = GetWinampValue(values, "PlaylistCount") ?? "-";
        var bitrate = GetWinampValue(values, "BitrateKbps");
        var sampleRate = GetWinampValue(values, "SampleRateHz");

        var connected = _winampSource.IsRecentlyConnected;

        WinampTitleText.Text =
            !string.IsNullOrWhiteSpace(title)
                ? title
                : connected
                    ? LocalizationService.Translate("No title")
                    : LocalizationService.Translate("Nothing playing");

        WinampArtistText.Text =
            !string.IsNullOrWhiteSpace(artist)
                ? artist
                : connected
                    ? LocalizationService.Translate("Winamp connected")
                    : LocalizationService.Translate("Waiting for Winamp...");

        WinampAlbumText.Text = album ?? string.Empty;
        WinampPlaybackText.Text = connected
            ? localizedState
            : LocalizationService.Translate("Disconnected");
        WinampTimeText.Text = $"{elapsed} / {duration}";
        WinampPlaylistText.Text = $"{playlistPosition} / {playlistCount}";
        WinampAudioText.Text =
            bitrate is null && sampleRate is null
                ? "-"
                : $"{bitrate ?? "-"} kbps / {FormatSampleRate(sampleRate)}";

        WinampStatusText.Text = connected
            ? $"{LocalizationService.Translate("Connected")} • {localizedState}"
            : LocalizationService.Translate("Waiting for Winamp");

        const string winampPipe = @"\\.\pipe\LIS2ControlCenter.Winamp";

        WinampConnectionDetailText.Text = connected
            ? LocalizationService.Format("Receiving snapshots on {0}", winampPipe) +
              (_winampSource.LastSnapshotAt is not null
                  ? " • " + LocalizationService.Format(
                      "last update {0}",
                      _winampSource.LastSnapshotAt.Value.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture))
                  : string.Empty)
            : LocalizationService.Format(
                "Listening on {0} — no recent plugin/simulator data.",
                winampPipe);
    }

    private static string? GetWinampValue(
        IReadOnlyDictionary<string, object?> values,
        string key)
    {
        if (!values.TryGetValue(key, out var raw) || raw is null)
            return null;

        var text = Convert.ToString(raw, CultureInfo.CurrentCulture);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string FormatSampleRate(string? raw)
    {
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hz))
            return raw ?? "-";

        return hz >= 1000
            ? $"{hz / 1000.0:0.#} kHz"
            : $"{hz} Hz";
    }

    private void EventQueue_Changed(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(RefreshEventsView);
    }

    private void EventUiTimer_Tick(object? sender, EventArgs e)
    {
        if (MainTabs.SelectedIndex == 4)
            RefreshEventsView();
    }

    private async void AddEvent_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var priority = GetSelectedEventPriority();

            if (!int.TryParse(
                    EventDurationTextBox.Text,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var durationSeconds) ||
                durationSeconds is < 1 or > 3600)
            {
                throw new InvalidOperationException(
                    LocalizationService.Translate(
                        "Event duration must be between 1 and 3600 seconds."));
            }

            var line1 = EventLine1TextBox.Text ?? string.Empty;
            var line2 = EventLine2TextBox.Text ?? string.Empty;

            await QueueDisplayEventAsync(
                $"manual-{Guid.NewGuid():N}",
                line1,
                line2,
                priority,
                TimeSpan.FromSeconds(durationSeconds));

            Log(
                $"INFO display event queued: priority={priority}, " +
                $"duration={durationSeconds}s");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void TestWarningEvent_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await QueueDisplayEventAsync(
                $"warning-{Guid.NewGuid():N}",
                LocalizationService.Translate("!! WARNING !!"),
                LocalizationService.Translate("Test notification"),
                priority: 100,
                duration: TimeSpan.FromSeconds(8));

            Log("INFO warning event queued for 8 seconds");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void ClearEvents_Click(object sender, RoutedEventArgs e)
    {
        _eventQueue.Clear();
        await RenderRuntimePageAsync();
        RefreshEventsView();
        Log("INFO all display events cleared");
    }

    private async void CancelSelectedEvent_Click(object sender, RoutedEventArgs e)
    {
        if (EventQueueListBox.SelectedItem is not EventQueueRow row)
            return;

        if (_eventQueue.Remove(row.Id))
        {
            await RenderRuntimePageAsync();
            RefreshEventsView();
            Log($"INFO display event '{row.Id}' cancelled");
        }
    }

    private void EventQueueListBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (EventQueueListBox.SelectedItem is EventQueueRow row)
        {
            SelectedEventText.Text =
                $"{LocalizationService.Translate("ID")}: {row.Id}{Environment.NewLine}" +
                $"{LocalizationService.Translate("Priority")}: {row.PriorityName} ({row.Priority}) • " +
                $"{LocalizationService.Translate("Expires")}: {row.ExpiresAt.ToLocalTime():HH:mm:ss}";
        }
        else
        {
            SelectedEventText.Text =
                LocalizationService.Translate("Select an event to inspect it.");
        }
    }

    private void RefreshEventsView()
    {
        var now = DateTimeOffset.Now;
        var selectedId = (EventQueueListBox.SelectedItem as EventQueueRow)?.Id;

        var rows = _eventQueue
            .Snapshot(now)
            .Select(displayEvent =>
            {
                var remaining = displayEvent.ExpiresAt - now;

                return new EventQueueRow(
                    displayEvent.Id,
                    displayEvent.Frame.Line1,
                    displayEvent.Frame.Line2,
                    displayEvent.Priority,
                    FormatEventPriority(displayEvent.Priority),
                    displayEvent.ExpiresAt,
                    remaining <= TimeSpan.Zero
                        ? LocalizationService.Translate("expired")
                        : $"{Math.Ceiling(remaining.TotalSeconds):0}s");
            })
            .ToArray();

        EventQueueListBox.ItemsSource = rows;

        if (selectedId is not null)
        {
            EventQueueListBox.SelectedItem =
                rows.FirstOrDefault(row =>
                    string.Equals(
                        row.Id,
                        selectedId,
                        StringComparison.OrdinalIgnoreCase));
        }

        EventQueueSummaryText.Text =
            rows.Length == 1
                ? LocalizationService.Translate("1 event")
                : string.Format(
                    CultureInfo.CurrentCulture,
                    LocalizationService.Translate("{0} events"),
                    rows.Length);
    }

    private int GetSelectedEventPriority()
    {
        if (EventPriorityComboBox.SelectedItem is not ComboBoxItem { Tag: string tag } ||
            !int.TryParse(
                tag,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var priority))
        {
            throw new InvalidOperationException(
                LocalizationService.Translate("Select a valid event priority."));
        }

        return priority;
    }

    private async Task QueueDisplayEventAsync(
        string id,
        string line1,
        string line2,
        int priority,
        TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration));

        var now = DateTimeOffset.Now;

        _eventQueue.Add(
            new DisplayEvent(
                id,
                DisplayFrame.Create(line1, line2),
                priority,
                now.Add(duration)));

        await RenderRuntimePageAsync();
        RefreshEventsView();
    }

    private static string FormatEventPriority(int priority) =>
        LocalizationService.Translate(
            priority switch
            {
                >= 200 => "Critical",
                >= 100 => "Warning",
                >= 50 => "Notice",
                _ => "Info"
            });

    private void HardwareTimer_Tick(object? sender, EventArgs e)
    {
        if (MainTabs.SelectedIndex == 5)
            RefreshHardwareSensors();
    }

    private void HardwareFilterChanged(object sender, EventArgs e)
    {
        if (IsLoaded)
            RefreshHardwareSensors();
    }

    private void RefreshHardware_Click(object sender, RoutedEventArgs e)
    {
        RefreshHardwareSensors();
        RefreshFanSensorChoices();
    }

    private void HardwareSensorsListBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (HardwareSensorsListBox.SelectedItem is HardwareSensorRow sensor)
        {
            SelectedHardwareSensorText.Text =
                $"{sensor.Name} — {sensor.DisplayValue}{Environment.NewLine}{sensor.Key}";
        }
        else
        {
            SelectedHardwareSensorText.Text =
                LocalizationService.Translate("Select a sensor above.");
        }
    }

    private void RefreshHardwareSensors()
    {
        var snapshot = _sources.Snapshot();
        var selectedKey =
            (HardwareSensorsListBox.SelectedItem as HardwareSensorRow)?.Key;

        var search = HardwareSearchTextBox.Text?.Trim() ?? string.Empty;
        var typeFilter =
            HardwareTypeComboBox.SelectedItem is ComboBoxItem { Tag: string tag }
                ? tag
                : "All";

        var rows = snapshot
            .Where(pair =>
                pair.Key.StartsWith("Hardware.", StringComparison.OrdinalIgnoreCase) &&
                !pair.Key.EndsWith(".Unit", StringComparison.OrdinalIgnoreCase) &&
                IsNumericValue(pair.Value))
            .Select(pair => CreateHardwareSensorRow(pair.Key, pair.Value, snapshot))
            .Where(row =>
                MatchesHardwareTypeFilter(row.TypeKey, typeFilter) &&
                (string.IsNullOrWhiteSpace(search) ||
                 row.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                 row.Key.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                 row.TypeKey.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                 row.Type.Contains(search, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(row => row.Type, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        HardwareSensorsListBox.ItemsSource = rows;

        if (selectedKey is not null)
        {
            HardwareSensorsListBox.SelectedItem =
                rows.FirstOrDefault(row =>
                    string.Equals(
                        row.Key,
                        selectedKey,
                        StringComparison.OrdinalIgnoreCase));
        }

        var hardwareError = _sources.Errors.TryGetValue("Hardware", out var error)
            ? error
            : null;

        var totalHardwareValues = snapshot.Count(pair =>
            pair.Key.StartsWith("Hardware.", StringComparison.OrdinalIgnoreCase) &&
            !pair.Key.EndsWith(".Unit", StringComparison.OrdinalIgnoreCase));

        HardwareSummaryText.Text =
            !string.IsNullOrWhiteSpace(hardwareError)
                ? LocalizationService.Format("Hardware source error: {0}", hardwareError)
                : LocalizationService.Format(
                    "{0} visible sensor(s) / {1} total values",
                    rows.Length,
                    totalHardwareValues);
    }

    private static HardwareSensorRow CreateHardwareSensorRow(
        string key,
        object? rawValue,
        IReadOnlyDictionary<string, object?> snapshot)
    {
        var parts = key.Split('.');
        var hardwareName = parts.Length > 1
            ? HumanizeSensorName(parts[1])
            : "Hardware";
        var type = parts.Length > 2
            ? parts[2]
            : "Other";
        var sensorName = parts.Length > 3
            ? HumanizeSensorName(string.Join(" ", parts.Skip(3)))
            : key;

        var value = TryConvertToDouble(rawValue, out var number) && number is not null
            ? number.Value.ToString("0.##", CultureInfo.CurrentCulture)
            : Convert.ToString(rawValue, CultureInfo.CurrentCulture) ?? "-";

        var unit = snapshot.TryGetValue($"{key}.Unit", out var rawUnit)
            ? Convert.ToString(rawUnit, CultureInfo.CurrentCulture) ?? string.Empty
            : string.Empty;

        return new HardwareSensorRow(
            key,
            $"{hardwareName} — {sensorName}",
            type,
            LocalizationService.Translate(type),
            value,
            unit);
    }

    private static string HumanizeSensorName(string value) =>
        value.Replace('_', ' ').Trim();

    private static bool MatchesHardwareTypeFilter(
        string sensorType,
        string filter)
    {
        if (string.Equals(filter, "All", StringComparison.OrdinalIgnoreCase))
            return true;

        var knownTypes = new HashSet<string>(
            new[]
            {
                "Temperature",
                "Load",
                "Fan",
                "Clock",
                "Voltage",
                "Power",
                "Data",
                "Throughput"
            },
            StringComparer.OrdinalIgnoreCase);

        if (string.Equals(filter, "Other", StringComparison.OrdinalIgnoreCase))
            return !knownTypes.Contains(sensorType);

        return string.Equals(
            sensorType,
            filter,
            StringComparison.OrdinalIgnoreCase);
    }

    private async void AssignHardwareSensorToFan_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            if (HardwareSensorsListBox.SelectedItem is not HardwareSensorRow sensor)
                throw new InvalidOperationException(
                    LocalizationService.Translate("Select a hardware sensor first."));

            if (sender is not System.Windows.Controls.Button { Tag: string tag } ||
                !int.TryParse(tag, out var fanIndex) ||
                fanIndex < 0 ||
                fanIndex >= _settings.Fans.Channels.Length)
            {
                return;
            }

            var channel = _settings.Fans.Channels[fanIndex];
            channel.SensorKey = sensor.Key;

            await _settingsStore.SaveAsync(_settings);

            FanChannelsListBox.SelectedIndex = fanIndex;
            RefreshFanSensorChoices();

            if (FanChannelsListBox.SelectedIndex == fanIndex)
                FanSensorComboBox.Text = sensor.Key;

            Log(
                $"INFO assigned hardware sensor '{sensor.Key}' to " +
                $"{channel.Name}");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void RefreshFanSensorChoices()
    {
        var current = FanSensorComboBox.Text;

        var sensorKeys = _sources.Snapshot()
            .Where(pair =>
                pair.Key.StartsWith("Hardware.", StringComparison.OrdinalIgnoreCase) &&
                !pair.Key.EndsWith(".Unit", StringComparison.OrdinalIgnoreCase) &&
                IsNumericValue(pair.Value))
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Key)
            .ToArray();

        FanSensorComboBox.ItemsSource = sensorKeys;
        FanSensorComboBox.Text = current;
    }

    private static bool IsNumericValue(object? value)
    {
        if (value is null)
            return false;

        try
        {
            _ = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (
            ex is FormatException or InvalidCastException or OverflowException)
        {
            return false;
        }
    }

    private async void SaveFanChannel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (FanChannelsListBox.SelectedItem is not FanChannelSettings channel)
                throw new InvalidOperationException(
                    LocalizationService.Translate("Select a fan channel first."));

            var fixedPercent = ParsePercent(
                FanFixedTextBox.Text,
                LocalizationService.Translate("Fixed output"));
            var minimumPercent = ParsePercent(
                FanMinimumTextBox.Text,
                LocalizationService.Translate("Minimum output"));
            var maximumPercent = ParsePercent(
                FanMaximumTextBox.Text,
                LocalizationService.Translate("Maximum output"));
            var failSafePercent = ParsePercent(
                FanFailSafeTextBox.Text,
                LocalizationService.Translate("Fail-safe output"));

            if (!double.TryParse(
                    FanHysteresisTextBox.Text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var hysteresis) ||
                hysteresis < 0 ||
                hysteresis > 20)
            {
                throw new InvalidOperationException("Hysteresis must be between 0 and 20 degrees.");
            }

            if (minimumPercent > maximumPercent)
                throw new InvalidOperationException(
                    LocalizationService.Translate(
                        "Minimum fan output must not be greater than maximum output."));

            var mode = GetSelectedFanMode();

            if (!Enum.TryParse<FanMode>(mode, ignoreCase: true, out _))
                throw new InvalidOperationException(
                    LocalizationService.Format("Unknown fan mode '{0}'.", mode));

            var curve = ParseFanCurve(FanCurveTextBox.Text);

            if (string.Equals(mode, nameof(FanMode.Curve), StringComparison.OrdinalIgnoreCase) &&
                curve.Count == 0)
            {
                throw new InvalidOperationException(
                    LocalizationService.Translate(
                        "Curve mode requires at least one temperature/output point."));
            }

            channel.Name = string.IsNullOrWhiteSpace(FanNameTextBox.Text)
                ? LocalizationService.Format(
                    "Fan {0}",
                    FanChannelsListBox.SelectedIndex + 1)
                : FanNameTextBox.Text.Trim();
            channel.Mode = mode;
            channel.SensorKey = string.IsNullOrWhiteSpace(FanSensorComboBox.Text)
                ? null
                : FanSensorComboBox.Text.Trim();
            channel.FixedPercent = fixedPercent;
            channel.MinimumPercent = minimumPercent;
            channel.MaximumPercent = maximumPercent;
            channel.FailSafePercent = failSafePercent;
            channel.HysteresisDegrees = hysteresis;
            channel.AllowStop = FanAllowStopCheckBox.IsChecked == true;
            channel.Curve = curve;
            RefreshFanCurvePreview(curve);

            await _settingsStore.SaveAsync(_settings);

            _lastAutomaticFanOutputs = null;
            Array.Clear(_lastAutomaticFanSensorValues);
            FanChannelsListBox.Items.Refresh();

            Log($"INFO saved fan channel '{channel.Name}'");

            if (_settings.Fans.AutomaticControlEnabled)
                await ApplyAutomaticFanControlAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void RefreshFanLiveStatus()
    {
        if (FanLiveStatusText is null)
            return;

        var index = FanChannelsListBox.SelectedIndex;
        if (index < 0 || index >= 4)
        {
            FanLiveStatusText.Text = "-";
            return;
        }

        var channel = _settings.Fans.Channels[index];
        double? sensor = null;
        var snapshot = _sources.Snapshot();
        if (!string.IsNullOrWhiteSpace(channel.SensorKey) &&
            snapshot.TryGetValue(channel.SensorKey, out var rawSensor))
        {
            TryConvertToDouble(rawSensor, out sensor);
        }

        var output = _lastAutomaticFanOutputs?[index];
        FanLiveStatusText.Text =
            $"Sensor: {(sensor is null ? "-" : sensor.Value.ToString("0.##", CultureInfo.CurrentCulture))}  |  " +
            $"Output: {(output is null ? "-" : output.Value.ToString(CultureInfo.CurrentCulture) + "%")}";
    }

    private void FanCurveTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded || FanCurvePreviewCanvas is null)
            return;

        try
        {
            RefreshFanCurvePreview(ParseFanCurve(FanCurveTextBox.Text));
        }
        catch
        {
            FanCurvePreviewCanvas.Children.Clear();
        }
    }

    private void RefreshFanCurvePreview(IEnumerable<FanCurvePointSettings> curve)
    {
        if (FanCurvePreviewCanvas is null)
            return;

        FanCurvePreviewCanvas.Children.Clear();
        var points = curve.OrderBy(point => point.Temperature).ToArray();
        if (points.Length == 0)
            return;

        const double width = 360;
        const double height = 120;
        var minTemp = Math.Min(0, points.Min(point => point.Temperature));
        var maxTemp = Math.Max(minTemp + 1, points.Max(point => point.Temperature));

        var polyline = new System.Windows.Shapes.Polyline
        {
            Stroke = (System.Windows.Media.Brush)FindResource("AccentBrush"),
            StrokeThickness = 2
        };

        foreach (var point in points)
        {
            var x = (point.Temperature - minTemp) / (maxTemp - minTemp) * width;
            var y = height - Math.Clamp(point.OutputPercent, 0, 100) / 100.0 * height;
            polyline.Points.Add(new System.Windows.Point(x, y));
        }

        FanCurvePreviewCanvas.Children.Add(polyline);
    }

    private static List<FanCurvePointSettings> ParseFanCurve(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<FanCurvePointSettings>();

        var points = new List<FanCurvePointSettings>();

        foreach (var token in text.Split(
                     ';',
                     StringSplitOptions.RemoveEmptyEntries |
                     StringSplitOptions.TrimEntries))
        {
            var parts = token.Split(
                ':',
                StringSplitOptions.TrimEntries);

            if (parts.Length != 2 ||
                !double.TryParse(
                    parts[0],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var temperature) ||
                !int.TryParse(
                    parts[1],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var output) ||
                output is < 0 or > 100)
            {
                throw new InvalidOperationException(
                    LocalizationService.Format(
                        "Invalid curve point '{0}'. Use temperature:percent, e.g. 60:80.",
                        token));
            }

            points.Add(new FanCurvePointSettings
            {
                Temperature = temperature,
                OutputPercent = output
            });
        }

        return points
            .OrderBy(point => point.Temperature)
            .ToList();
    }

    private static string FormatFanCurve(
        IEnumerable<FanCurvePointSettings> curve) =>
        string.Join(
            ";",
            curve
                .OrderBy(point => point.Temperature)
                .Select(point =>
                    $"{point.Temperature.ToString("0.##", CultureInfo.InvariantCulture)}:" +
                    $"{point.OutputPercent.ToString(CultureInfo.InvariantCulture)}"));

    private async void PageTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            await RenderRuntimePageAsync();
        }
        catch (Exception ex)
        {
            Log($"ERR  page runtime: {ex.Message}");
        }
    }

    private void LogSourceHealth()
    {
        foreach (var source in _sources.Sources)
        {
            var error = _sources.Errors[source.Id];

            if (string.IsNullOrWhiteSpace(error))
                Log($"INFO source '{source.Id}' started");
            else
                Log($"WARN source '{source.Id}' failed: {error}");
        }
    }

    private async Task RenderRuntimePageAsync()
    {
        var nextFrame = _displayRuntime.RenderNext(
            _sources.Snapshot(),
            DateTimeOffset.Now);

        if (nextFrame is null)
            return;

        _frame = nextFrame;
        await WriteFrameAsync(nextFrame);
        RefreshPreview();

        _pageTimer.Interval = _displayRuntime.SuggestedDuration;
    }

    private async Task WriteFrameAsync(DisplayFrame frame)
    {
        if (_frameWriter is null)
            throw new InvalidOperationException(
                LocalizationService.Translate("LIS2 display writer is not initialized."));

        await _frameWriter.WriteAsync(frame);
    }

    private void LoadPagesIntoRuntime()
    {
        _pageScheduler.ReplacePages(
            _settings.Pages
                .Where(page => page.Enabled)
                .Select(page =>
                    new DisplayPage(
                        page.Id,
                        page.Name,
                        page.Line1Template,
                        page.Line2Template,
                        TimeSpan.FromSeconds(Math.Max(1, page.DurationSeconds)),
                        page.Priority,
                        page.VisibilityExpression,
                        ParseOverflowMode(page.Line1OverflowMode),
                        ParseOverflowMode(page.Line2OverflowMode),
                        TimeSpan.FromMilliseconds(Math.Clamp(page.ScrollStepMilliseconds, 50, 5000)),
                        TimeSpan.FromMilliseconds(Math.Clamp(page.ScrollEdgePauseMilliseconds, 0, 10000)))));

        _displayRuntime.ResetPageSelection();
    }

    private void BindPages()
    {
        PagesListBox.ItemsSource = null;
        PagesListBox.ItemsSource = _settings.Pages;

        if (_settings.Pages.Count > 0 && PagesListBox.SelectedIndex < 0)
            PagesListBox.SelectedIndex = 0;
    }

    private async void PageEnabledChanged(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded ||
            sender is not System.Windows.Controls.CheckBox { DataContext: PageDefinition page })
        {
            return;
        }

        PagesListBox.SelectedItem = page;

        await PersistPagesAsync();
        LoadPagesIntoRuntime();
        await RenderRuntimePageAsync();

        Log(
            $"INFO page '{page.Name}' " +
            (page.Enabled ? "enabled" : "disabled"));
    }

    private void PagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PagesListBox.SelectedItem is not PageDefinition page)
            return;

        PageNameTextBox.Text = page.Name;
        PageLine1TextBox.Text = page.Line1Template;
        PageLine2TextBox.Text = page.Line2Template;
        PageDurationTextBox.Text = page.DurationSeconds.ToString(CultureInfo.InvariantCulture);
        PageVisibilityTextBox.Text = page.VisibilityExpression ?? string.Empty;
        SelectComboBoxTag(PageLine1OverflowComboBox, page.Line1OverflowMode);
        SelectComboBoxTag(PageLine2OverflowComboBox, page.Line2OverflowMode);
        PageScrollSpeedTextBox.Text = page.ScrollStepMilliseconds.ToString(CultureInfo.InvariantCulture);
        PageEdgePauseTextBox.Text = page.ScrollEdgePauseMilliseconds.ToString(CultureInfo.InvariantCulture);
        RefreshPageEditorPreview();
    }

    private async void AddPage_Click(object sender, RoutedEventArgs e)
    {
        var page = new PageDefinition
        {
            Name = LocalizationService.Translate("New page"),
            Line1Template = LocalizationService.Translate("New page"),
            Line2Template = "{Clock.Time}",
            DurationSeconds = 5
        };

        _settings.Pages.Add(page);
        await PersistPagesAsync();
        BindPages();
        PagesListBox.SelectedItem = page;
    }

    private async void DuplicatePage_Click(object sender, RoutedEventArgs e)
    {
        if (PagesListBox.SelectedItem is not PageDefinition source)
            return;

        var copy = new PageDefinition
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = source.Name + " copy",
            Line1Template = source.Line1Template,
            Line2Template = source.Line2Template,
            DurationSeconds = source.DurationSeconds,
            Enabled = source.Enabled,
            Priority = source.Priority,
            VisibilityExpression = source.VisibilityExpression,
            Line1OverflowMode = source.Line1OverflowMode,
            Line2OverflowMode = source.Line2OverflowMode,
            ScrollStepMilliseconds = source.ScrollStepMilliseconds,
            ScrollEdgePauseMilliseconds = source.ScrollEdgePauseMilliseconds
        };

        var index = _settings.Pages.IndexOf(source);
        _settings.Pages.Insert(index + 1, copy);
        await PersistPagesAsync();
        BindPages();
        LoadPagesIntoRuntime();
        PagesListBox.SelectedItem = copy;
    }

    private async void MovePageUp_Click(object sender, RoutedEventArgs e) =>
        await MoveSelectedPageAsync(-1);

    private async void MovePageDown_Click(object sender, RoutedEventArgs e) =>
        await MoveSelectedPageAsync(1);

    private async Task MoveSelectedPageAsync(int delta)
    {
        var index = PagesListBox.SelectedIndex;
        var target = index + delta;

        if (index < 0 || target < 0 || target >= _settings.Pages.Count)
            return;

        var page = _settings.Pages[index];
        _settings.Pages.RemoveAt(index);
        _settings.Pages.Insert(target, page);

        await PersistPagesAsync();
        BindPages();
        LoadPagesIntoRuntime();
        PagesListBox.SelectedIndex = target;
    }

    private void PageEditorChanged(object sender, EventArgs e)
    {
        if (IsLoaded)
            RefreshPageEditorPreview();
    }

    private void RefreshPageEditorPreview()
    {
        if (PageEditorPreviewLine1 is null || PageEditorPreviewLine2 is null)
            return;

        var renderer = new TemplateRenderer();
        var values = _sources.Snapshot();
        PageEditorPreviewLine1.Text = DisplayFrame.Normalize(
            renderer.Render(PageLine1TextBox.Text ?? string.Empty, values));
        PageEditorPreviewLine2.Text = DisplayFrame.Normalize(
            renderer.Render(PageLine2TextBox.Text ?? string.Empty, values));
    }

    private static DisplayOverflowMode ParseOverflowMode(string? value) =>
        Enum.TryParse<DisplayOverflowMode>(value, true, out var mode)
            ? mode
            : DisplayOverflowMode.PingPong;

    private static string GetComboBoxTag(ComboBox comboBox, string fallback) =>
        comboBox.SelectedItem is ComboBoxItem { Tag: string tag } ? tag : fallback;

    private static void SelectComboBoxTag(ComboBox comboBox, string? value)
    {
        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(Convert.ToString(item.Tag), value, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedIndex = 0;
    }

    private async void DeletePage_Click(object sender, RoutedEventArgs e)
    {
        if (PagesListBox.SelectedItem is not PageDefinition page)
            return;

        _settings.Pages.Remove(page);
        _settings.EnsureDefaults();

        await PersistPagesAsync();
        BindPages();
        LoadPagesIntoRuntime();
    }

    private async void SavePage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (PagesListBox.SelectedItem is not PageDefinition page)
                throw new InvalidOperationException(
                    LocalizationService.Translate("Select a page first."));

            if (!int.TryParse(PageDurationTextBox.Text, out var duration) || duration < 1)
                throw new InvalidOperationException(
                    LocalizationService.Translate("Page duration must be at least 1 second."));

            page.Name = string.IsNullOrWhiteSpace(PageNameTextBox.Text)
                ? LocalizationService.Translate("Page")
                : PageNameTextBox.Text.Trim();
            page.Line1Template = PageLine1TextBox.Text;
            page.Line2Template = PageLine2TextBox.Text;
            page.DurationSeconds = duration;
            page.VisibilityExpression = string.IsNullOrWhiteSpace(PageVisibilityTextBox.Text)
                ? null
                : PageVisibilityTextBox.Text.Trim();
            page.Line1OverflowMode = GetComboBoxTag(PageLine1OverflowComboBox, "PingPong");
            page.Line2OverflowMode = GetComboBoxTag(PageLine2OverflowComboBox, "PingPong");

            if (!int.TryParse(PageScrollSpeedTextBox.Text, out var scrollSpeed) ||
                scrollSpeed is < 50 or > 5000)
                throw new InvalidOperationException("Scroll step must be between 50 and 5000 ms.");

            if (!int.TryParse(PageEdgePauseTextBox.Text, out var edgePause) ||
                edgePause is < 0 or > 10000)
                throw new InvalidOperationException("Edge pause must be between 0 and 10000 ms.");

            page.ScrollStepMilliseconds = scrollSpeed;
            page.ScrollEdgePauseMilliseconds = edgePause;

            await PersistPagesAsync();
            LoadPagesIntoRuntime();
            BindPages();
            PagesListBox.SelectedItem = page;

            Log($"INFO saved page '{page.Name}'");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task PersistPagesAsync()
    {
        await _settingsStore.SaveAsync(_settings);
    }

    private async void TestEvent_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await QueueDisplayEventAsync(
                $"page-test-{Guid.NewGuid():N}",
                LocalizationService.Translate("** EVENT TEST **"),
                LocalizationService.Translate("Overlay for 5 sec"),
                priority: 100,
                duration: TimeSpan.FromSeconds(5));

            Log("INFO event overlay queued for 5 seconds");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void BindCustomGlyphs()
    {
        CustomGlyphSlotComboBox.ItemsSource = Enumerable.Range(1, 8).ToArray();
        if (CustomGlyphSlotComboBox.SelectedIndex < 0)
            CustomGlyphSlotComboBox.SelectedIndex = 0;
        LoadSelectedGlyphIntoEditor();
    }

    private void CustomGlyphSlotChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            LoadSelectedGlyphIntoEditor();
    }

    private void LoadSelectedGlyphIntoEditor()
    {
        if (CustomGlyphSlotComboBox.SelectedItem is not int slot ||
            slot < 1 || slot > _settings.CustomGlyphs.Count)
            return;

        var glyph = _settings.CustomGlyphs[slot - 1];
        CustomGlyphNameTextBox.Text = glyph.Name;
        CustomGlyphRowsTextBox.Text = string.Join(
            Environment.NewLine,
            glyph.Rows.Select(row => Convert.ToString(row, 2).PadLeft(5, '0')));
        RefreshGlyphPreview(glyph.Rows);
    }

    private byte[] ReadGlyphEditor()
    {
        var lines = (CustomGlyphRowsTextBox.Text ?? string.Empty)
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        if (lines.Length != 8)
            throw new InvalidOperationException("A custom character needs exactly eight rows.");

        var rows = new byte[8];
        for (var index = 0; index < lines.Length; index++)
        {
            var row = lines[index].Trim();
            if (row.Length != 5 || row.Any(ch => ch is not ('0' or '1')))
                throw new InvalidOperationException("Each custom-character row must contain exactly five 0/1 pixels.");

            rows[index] = Convert.ToByte(row, 2);
        }

        return rows;
    }

    private async void SaveCustomGlyph_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CustomGlyphSlotComboBox.SelectedItem is not int slot)
                return;

            var rows = ReadGlyphEditor();
            var glyph = _settings.CustomGlyphs[slot - 1];
            glyph.Name = string.IsNullOrWhiteSpace(CustomGlyphNameTextBox.Text)
                ? $"Glyph {slot}"
                : CustomGlyphNameTextBox.Text.Trim();
            glyph.Rows = rows;

            await _settingsStore.SaveAsync(_settings);
            RefreshGlyphPreview(rows);
            Log($"INFO saved custom glyph {slot} '{glyph.Name}'");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void SendCustomGlyph_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CustomGlyphSlotComboBox.SelectedItem is not int slot)
                return;

            var rows = ReadGlyphEditor();
            if (_customCharacterManager is null)
                throw new InvalidOperationException(LocalizationService.Translate("LIS2 device is not connected."));

            await _customCharacterManager.ProgramSlotAsync(slot, rows, force: true);
            RefreshGlyphPreview(rows);
            Log($"INFO programmed custom glyph slot {slot}");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void SendAllCustomGlyphs_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_customCharacterManager is null)
                throw new InvalidOperationException(LocalizationService.Translate("LIS2 device is not connected."));

            await _customCharacterManager.ProgramAllAsync(
                _settings.CustomGlyphs.Select(glyph => glyph.Rows).ToArray(),
                force: true);
            Log("INFO programmed all eight custom glyph slots");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void CustomGlyphRowsChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded || CustomGlyphPreview is null)
            return;

        try
        {
            RefreshGlyphPreview(ReadGlyphEditor());
        }
        catch
        {
            CustomGlyphPreview.Text = "Invalid 5x8 bitmap";
        }
    }

    private void RefreshGlyphPreview(IReadOnlyList<byte> rows)
    {
        if (CustomGlyphPreview is null)
            return;

        CustomGlyphPreview.Text = string.Join(
            Environment.NewLine,
            rows.Select(row =>
                new string(
                    Enumerable.Range(0, 5)
                        .Select(column => (row & (1 << (4 - column))) != 0 ? '█' : '·')
                        .ToArray())));
    }

    private void ApplySettingsToUi()
    {
        TransportModeComboBox.SelectedIndex =
            string.Equals(_settings.TransportMode, "Serial", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        if (_settings.PortName is not null && PortComboBox.Items.Contains(_settings.PortName))
            PortComboBox.SelectedItem = _settings.PortName;

        var themeMode = Enum.TryParse<AppThemeMode>(
            _settings.ThemeMode,
            ignoreCase: true,
            out var parsedTheme)
            ? parsedTheme
            : AppThemeMode.System;

        foreach (var item in ThemeModeComboBox.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is string tag &&
                string.Equals(tag, themeMode.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                ThemeModeComboBox.SelectedItem = item;
                break;
            }
        }

        ThemeService.Apply(themeMode);

        var languageMode = Enum.TryParse<AppLanguageMode>(
            _settings.LanguageMode,
            ignoreCase: true,
            out var parsedLanguage)
            ? parsedLanguage
            : AppLanguageMode.System;

        foreach (var item in LanguageModeComboBox.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is string tag &&
                string.Equals(tag, languageMode.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                LanguageModeComboBox.SelectedItem = item;
                break;
            }
        }

        LocalizationService.Apply(languageMode);
        LocalizationService.ApplyTo(this);
        RefreshLocalizedSectionHeader();
        UpdateNavigationSelection(MainTabs.SelectedIndex);
        UpdateTransportUi();
        UpdateFanOutputFields(
            _settings.Fans.Channels
                .Select(channel => channel.FixedPercent)
                .ToArray());
    }

    private async void ThemeModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded ||
            ThemeModeComboBox.SelectedItem is not ComboBoxItem { Tag: string tag } ||
            !Enum.TryParse<AppThemeMode>(tag, ignoreCase: true, out var mode))
        {
            return;
        }

        _settings.ThemeMode = mode.ToString();
        ThemeService.Apply(mode);
        UpdateNavigationSelection(MainTabs.SelectedIndex);

        await _settingsStore.SaveAsync(_settings);

        Log(
            mode == AppThemeMode.System
                ? $"INFO theme set to System default ({(ThemeService.IsDarkEffective ? "Dark" : "Light")})"
                : $"INFO theme set to {mode}");
    }

    private async void LanguageModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded ||
            LanguageModeComboBox.SelectedItem is not ComboBoxItem { Tag: string tag } ||
            !Enum.TryParse<AppLanguageMode>(tag, ignoreCase: true, out var mode))
        {
            return;
        }

        _settings.LanguageMode = mode.ToString();
        LocalizationService.Apply(mode);
        LocalizationService.ApplyTo(this);
        RefreshLocalizedSectionHeader();
        RefreshWinampView();
        RefreshEventsView();
        RefreshHardwareSensors();
        RefreshFanSensorChoices();
        FanChannelsListBox.Items.Refresh();
        UpdateTransportUi();
        UpdateConnectionUiLocalization();
        RefreshDiagnostics();
        _trayIcon.ApplyLocalization();

        await _settingsStore.SaveAsync(_settings);

        Log(
            $"INFO language set to {mode} " +
            $"({LocalizationService.LanguageCode})");
    }

    private void RefreshLocalizedSectionHeader()
    {
        var index = MainTabs.SelectedIndex;
        if (index < 0 || index >= Sections.Length)
            return;

        SectionTitleText.Text = LocalizationService.Translate(Sections[index].Title);
        SectionSubtitleText.Text = LocalizationService.Translate(Sections[index].Subtitle);
    }

    private void RefreshPorts()
    {
        var selected = PortComboBox.SelectedItem as string ?? _settings.PortName;
        var ports = SerialPort.GetPortNames()
            .OrderBy(port => port, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        PortComboBox.ItemsSource = ports;

        if (selected is not null && ports.Contains(selected))
            PortComboBox.SelectedItem = selected;
        else if (ports.Length > 0)
            PortComboBox.SelectedIndex = 0;
    }

    private async Task ReconnectAsync()
    {
        if (_device is not null)
        {
            await _device.DisposeAsync();
            _device = null;
            _frameWriter = null;
            _transport = null;
        }

        if (string.Equals(_settings.TransportMode, "Serial", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(_settings.PortName))
                throw new InvalidOperationException(
                    LocalizationService.Translate(
                        "Select a COM port before using the serial transport."));

            _transport = new SerialLis2Transport(_settings.PortName);
            VirtualStateText.Text = LocalizationService.Translate(
                "Virtual state is unavailable while Serial transport is active.");
        }
        else
        {
            var virtualTransport = new VirtualLis2Transport();
            virtualTransport.Written += Transport_Written;
            virtualTransport.StateChanged += VirtualTransport_StateChanged;
            _transport = virtualTransport;
            UpdateVirtualState(virtualTransport.State);
        }

        _device = new Lis2Device(_transport);
        await _device.ConnectAsync();
        _frameWriter = new DisplayFrameWriter(_device);
        _customCharacterManager = new CustomCharacterManager(_device);

        UpdateConnectionUiLocalization();

        Log($"INFO connected using {_settings.TransportMode} transport");
        _trayIcon.SetStatus(ConnectionText.Text);
    }

    private void VirtualTransport_StateChanged(object? sender, EventArgs e)
    {
        if (sender is VirtualLis2Transport transport)
            Dispatcher.Invoke(() => UpdateVirtualState(transport.State));
    }

    private void UpdateVirtualState(VirtualLis2State state)
    {
        VirtualStateText.Text =
            LocalizationService.Format(
                "Brightness: {0}",
                FormatBrightness(state.Brightness)) +
            Environment.NewLine +
            LocalizationService.Format(
                "Fans: {0}% / {1}% / {2}% / {3}%",
                state.Fan1,
                state.Fan2,
                state.Fan3,
                state.Fan4);
    }

    private static string FormatBrightness(Lis2Brightness brightness) =>
        brightness switch
        {
            Lis2Brightness.Percent100 => "100%",
            Lis2Brightness.Percent75 => "75%",
            Lis2Brightness.Percent50 => "50%",
            Lis2Brightness.Percent25 => "25%",
            _ => "?"
        };

    private void TransportModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        UpdateTransportUi();
    }

    private void UpdateTransportUi()
    {
        var serial = TransportModeComboBox.SelectedIndex == 1;
        PortComboBox.IsEnabled = serial;
    }

    private void UpdateConnectionUiLocalization()
    {
        var connected = _device?.IsConnected == true;

        if (string.Equals(_settings.TransportMode, "Serial", StringComparison.OrdinalIgnoreCase))
        {
            ConnectionText.Text = connected
                ? LocalizationService.Format(
                    "Connected: {0}",
                    _settings.PortName ?? "-")
                : LocalizationService.Translate("Disconnected");

            TransportSummaryText.Text = LocalizationService.Format(
                "Serial: {0}",
                _settings.PortName ?? "-");

            VirtualStateText.Text = LocalizationService.Translate(
                "Virtual state is unavailable while Serial transport is active.");
        }
        else
        {
            ConnectionText.Text = connected
                ? LocalizationService.Translate("Virtual LIS2 connected")
                : LocalizationService.Translate("Disconnected");

            TransportSummaryText.Text =
                LocalizationService.Translate("Virtual LIS2 transport");

            if (_transport is VirtualLis2Transport virtualTransport)
                UpdateVirtualState(virtualTransport.State);
        }

        _trayIcon.SetStatus(ConnectionText.Text);
    }

    private static string LocalizeTransportMode(string mode) =>
        string.Equals(mode, "Serial", StringComparison.OrdinalIgnoreCase)
            ? LocalizationService.Translate("Serial")
            : LocalizationService.Translate("Virtual");

    private static string LocalizeBoolean(bool value) =>
        LocalizationService.Translate(value ? "Yes" : "No");

    private void RefreshPorts_Click(object sender, RoutedEventArgs e) => RefreshPorts();

    private async void ApplyDeviceSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings.TransportMode = TransportModeComboBox.SelectedIndex == 1 ? "Serial" : "Virtual";
            _settings.PortName = PortComboBox.SelectedItem as string;
            await _settingsStore.SaveAsync(_settings);
            await ReconnectAsync();
            await WriteFrameAsync(_frame);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e) =>
        PortComboBox.Focus();

    private void Diagnostics_Click(object sender, RoutedEventArgs e)
    {
        RefreshDiagnostics();
        DiagnosticsTextBox.Focus();
    }

    private void RefreshDiagnostics_Click(object sender, RoutedEventArgs e) =>
        RefreshDiagnostics();

    private void RefreshDiagnostics()
    {
        var snapshot = _sources.Snapshot();
        var lines = new List<string>
        {
            LocalizationService.Format(
                "Time: {0}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)),
            LocalizationService.Format(
                "Transport: {0}",
                LocalizeTransportMode(_settings.TransportMode)),
            LocalizationService.Format(
                "Connected: {0}",
                LocalizeBoolean(_device?.IsConnected == true)),
            LocalizationService.Format("Port: {0}", _settings.PortName ?? "-"),
            LocalizationService.Format("Frame line 1: {0}", _frame.Line1),
            LocalizationService.Format("Frame line 2: {0}", _frame.Line2),
            LocalizationService.Format(
                "Automatic fan control: {0}",
                LocalizeBoolean(_settings.Fans.AutomaticControlEnabled)),
            LocalizationService.Format("Data values: {0}", snapshot.Count),
            ""
        };

        foreach (var source in _sources.Sources.OrderBy(source => source.Id))
        {
            var error = _sources.Errors[source.Id];
            lines.Add(
                string.IsNullOrWhiteSpace(error)
                    ? LocalizationService.Format(
                        "Source {0}: OK ({1} values)",
                        source.Id,
                        source.Values.Count)
                    : LocalizationService.Format(
                        "Source {0}: ERROR - {1}",
                        source.Id,
                        error));
        }

        lines.Add("");

        for (var index = 0; index < _settings.Fans.Channels.Length; index++)
        {
            var channel = _settings.Fans.Channels[index];
            lines.Add(
                LocalizationService.Format(
                    "Fan {0}: {1}, mode={2}, sensor={3}, fixed={4}%, min={5}%, max={6}%, fail-safe={7}%",
                    index + 1,
                    channel.Name,
                    LocalizationService.Translate(channel.Mode),
                    channel.SensorKey ?? "-",
                    channel.FixedPercent,
                    channel.MinimumPercent,
                    channel.MaximumPercent,
                    channel.FailSafePercent));
        }

        DiagnosticsTextBox.Text = string.Join(Environment.NewLine, lines);
    }

    private void StartWithWindowsChanged(object sender, RoutedEventArgs e)
    {
        if (_loadingStartupSetting || !IsLoaded)
            return;

        try
        {
            _startupService.SetEnabled(StartWithWindowsCheckBox.IsChecked == true);
            Log(StartWithWindowsCheckBox.IsChecked == true
                ? "INFO Windows autostart enabled"
                : "INFO Windows autostart disabled");
        }
        catch (Exception ex)
        {
            _loadingStartupSetting = true;
            StartWithWindowsCheckBox.IsChecked = _startupService.IsEnabled();
            _loadingStartupSetting = false;
            ShowError(ex);
        }
    }


    private async void SendLine1_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _frame = DisplayFrame.Create(Line1TextBox.Text, _frame.Line2);
            await WriteFrameAsync(_frame);
            RefreshPreview();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void SendLine2_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _frame = DisplayFrame.Create(_frame.Line1, Line2TextBox.Text);
            await WriteFrameAsync(_frame);
            RefreshPreview();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void RenderBoth_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _frame = DisplayFrame.Create(Line1TextBox.Text, Line2TextBox.Text);
            await WriteFrameAsync(_frame);
            RefreshPreview();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void Clear_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RequireDevice().ClearAsync();
            _frameWriter?.Reset();
            _frame = DisplayFrame.Create(string.Empty, string.Empty);
            RefreshPreview();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void Brightness_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not System.Windows.Controls.Button { Tag: string tag })
                return;

            var brightness = tag switch
            {
                "100" => Lis2Brightness.Percent100,
                "75" => Lis2Brightness.Percent75,
                "50" => Lis2Brightness.Percent50,
                "25" => Lis2Brightness.Percent25,
                _ => throw new InvalidOperationException(
                    LocalizationService.Translate("Unknown brightness."))
            };

            _settings.BrightnessPercent = int.Parse(tag, CultureInfo.InvariantCulture);
            await _settingsStore.SaveAsync(_settings);
            await RequireDevice().SetBrightnessAsync(brightness);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void Fans_Click(object sender, RoutedEventArgs e)
    {
        await ApplyManualFanValuesAsync(
            GetSliderPercent(Fan1Slider),
            GetSliderPercent(Fan2Slider),
            GetSliderPercent(Fan3Slider),
            GetSliderPercent(Fan4Slider));
    }

    private async void DashboardFans_Click(object sender, RoutedEventArgs e)
    {
        await ApplyManualFanValuesAsync(
            GetSliderPercent(DashboardFan1Slider),
            GetSliderPercent(DashboardFan2Slider),
            GetSliderPercent(DashboardFan3Slider),
            GetSliderPercent(DashboardFan4Slider));
    }

    private async Task ApplyManualFanValuesAsync(
        int fan1,
        int fan2,
        int fan3,
        int fan4)
    {
        try
        {
            var requested = new[] { fan1, fan2, fan3, fan4 };

            var outputs = new int[4];

            for (var index = 0; index < 4; index++)
            {
                var stored = _settings.Fans.Channels[index];
                stored.FixedPercent = requested[index];

                var configuration = new FanChannelConfiguration
                {
                    Mode = FanMode.Fixed,
                    FixedPercent = stored.FixedPercent,
                    MinimumPercent = stored.MinimumPercent,
                    MaximumPercent = stored.MaximumPercent,
                    FailSafePercent = stored.FailSafePercent,
                    AllowStop = stored.AllowStop
                };

                outputs[index] = _fanController.CalculateOutput(configuration);
            }

            await _settingsStore.SaveAsync(_settings);
            await RequireDevice().SetFansAsync(
                outputs[0],
                outputs[1],
                outputs[2],
                outputs[3]);

            UpdateFanOutputFields(outputs);

            Log($"INFO fan outputs after safety limits: {string.Join("/", outputs)}%");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void UpdateFanOutputFields(IReadOnlyList<int> outputs)
    {
        if (outputs.Count < 4)
            return;

        Fan1Slider.Value = outputs[0];
        Fan2Slider.Value = outputs[1];
        Fan3Slider.Value = outputs[2];
        Fan4Slider.Value = outputs[3];

        DashboardFan1Slider.Value = outputs[0];
        DashboardFan2Slider.Value = outputs[1];
        DashboardFan3Slider.Value = outputs[2];
        DashboardFan4Slider.Value = outputs[3];
    }

    private void Transport_Written(object? sender, VirtualLis2WriteEventArgs e)
    {
        var hex = string.Join(" ", e.Data.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
        Dispatcher.Invoke(() => Log($"TX   {hex}"));
    }

    private Lis2Device RequireDevice() =>
        _device?.IsConnected == true
            ? _device
            : throw new InvalidOperationException(
                LocalizationService.Translate("LIS2 device is not connected."));

    private void RefreshPreview()
    {
        PreviewLine1.Text = _frame.Line1;
        PreviewLine2.Text = _frame.Line2;
    }

    private static int GetSliderPercent(Slider slider) =>
        Math.Clamp(
            (int)Math.Round(slider.Value, MidpointRounding.AwayFromZero),
            0,
            100);

    private static int ParsePercent(string value, string label)
    {
        if (!int.TryParse(value, out var result) || result is < 0 or > 100)
            throw new InvalidOperationException(
                LocalizationService.Format(
                    "{0} must be between 0 and 100.",
                    label));

        return result;
    }

    private void ShowError(Exception ex)
    {
        Log($"ERR  {ex.Message}");
        System.Windows.MessageBox.Show(this, ex.Message, "LIS2 Control Center", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void Log(string text)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {text}{Environment.NewLine}";

        LogTextBox.AppendText(line);
        LogTextBox.ScrollToEnd();

    }
}
