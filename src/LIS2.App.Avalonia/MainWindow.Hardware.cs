using Avalonia.Controls;
using Avalonia.Threading;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly Dictionary<string, object?> _hardwareSensors = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PortableHardwareSensorPreference> _hardwarePreferences = new();
    private bool _hardwareListRebinding;
    private string? _selectedHardwareSensorKey;
    private string? _hardwareSourceError;
    // Hardware values feed display templates even when the Hardware view is not selected.
    private readonly DispatcherTimer _hardwareRefreshTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private bool _hardwareListLiveUpdates;

    private void InitializeHardwareBrowser()
    {
        this.FindControl<Button>("HardwareRefresh")!.Click += (_, _) => RefreshHardwareSensors();
        _hardwareRefreshTimer.Tick += (_, _) => RefreshHardwareSensors();
        this.FindControl<Button>("HardwareAutoRefresh")!.Click += (_, _) =>
        {
            // The button controls Hardware-view live updates, not the sensor
            // sampling required by page templates throughout the application.
            _hardwareListLiveUpdates = !_hardwareListLiveUpdates;
            if (_hardwareListLiveUpdates) RefreshHardwareSensors();
            SetUiButton("HardwareAutoRefresh", _hardwareListLiveUpdates ? "Stop live updates" : "Start live updates");
        };
        this.FindControl<TextBox>("HardwareSearch")!.TextChanged += (_, _) => FilterHardwareSensors();
        this.FindControl<ComboBox>("HardwareTypeFilter")!.SelectionChanged += (_, _) => FilterHardwareSensors();
        this.FindControl<CheckBox>("HardwareFavoritesOnly")!.IsCheckedChanged += (_, _) => FilterHardwareSensors();
        this.FindControl<TextBox>("HardwareAlias")!.PropertyChanged += (_, change) =>
        {
            if (change.Property == TextBox.TextProperty) MarkEditorChanged("HardwareSaveStatus");
        };
        this.FindControl<CheckBox>("HardwareFavorite")!.IsCheckedChanged += (_, _) => MarkEditorChanged("HardwareSaveStatus");
        this.FindControl<Button>("HardwareSavePreference")!.Click += async (_, _) =>
        {
            if (this.FindControl<ListBox>("HardwareSensorList")!.SelectedItem is not HardwareSensorEntry selected) return;
            var preference = _hardwarePreferences.FirstOrDefault(p => string.Equals(p.Key, selected.Key, StringComparison.OrdinalIgnoreCase));
            var created = preference is null;
            if (preference is null)
            {
                preference = new PortableHardwareSensorPreference { Key = selected.Key };
                _hardwarePreferences.Add(preference);
            }
            var oldAlias = preference.Alias;
            var oldFavorite = preference.IsFavorite;
            preference.Alias = this.FindControl<TextBox>("HardwareAlias")!.Text?.Trim() ?? "";
            preference.IsFavorite = this.FindControl<CheckBox>("HardwareFavorite")!.IsChecked == true;
            if (await PersistEditorAsync("HardwareSaveStatus"))
            {
                FilterHardwareSensors();
            }
            else
            {
                preference.Alias = oldAlias;
                preference.IsFavorite = oldFavorite;
                if (created)
                    _hardwarePreferences.Remove(preference);
            }
        };
        this.FindControl<ListBox>("HardwareSensorList")!.SelectionChanged += (_, _) =>
        {
            if (_hardwareListRebinding) return;
            if (this.FindControl<ListBox>("HardwareSensorList")!.SelectedItem is HardwareSensorGroupHeader)
            {
                this.FindControl<ListBox>("HardwareSensorList")!.SelectedItem = null;
                return;
            }
            var selected = this.FindControl<ListBox>("HardwareSensorList")!.SelectedItem as HardwareSensorEntry;
            _selectedHardwareSensorKey = selected?.Key;
            SetHardwareFanAssignmentEnabled(selected?.IsAvailable == true);
            this.FindControl<Button>("HardwareSavePreference")!.IsEnabled = selected is not null;
            var preference = _hardwarePreferences.FirstOrDefault(p => string.Equals(p.Key, selected?.Key, StringComparison.OrdinalIgnoreCase));
            this.FindControl<TextBox>("HardwareAlias")!.Text = preference?.Alias ?? "";
            this.FindControl<CheckBox>("HardwareFavorite")!.IsChecked = preference?.IsFavorite == true;
            SetUiText("HardwareSelectedSensor", selected is null ? "Select a sensor above." : (UiMessage)$"{selected.SelectionDetails}");
        };
        for (var channelIndex = 0; channelIndex < 4; channelIndex++)
        {
            var selectedChannel = channelIndex;
            var buttonName = channelIndex == 0 ? "HardwareUseForFan" : "HardwareUseForFan" + (channelIndex + 1);
            this.FindControl<Button>(buttonName)!.Click += (_, _) => AssignHardwareSensorToFan(selectedChannel);
        }
        foreach (var preset in new[] { "CPU", "GPU", "Memory", "Temperatures", "Fans" })
        {
            var selectedPreset = preset;
            this.FindControl<Button>("HardwarePreset" + preset)!.Click += async (_, _) =>
            {
                var page = HardwareDisplayPresetBuilder.Build(selectedPreset, _hardwareSensors, _hardwarePreferences);
                if (page is null)
                {
                    SetUiText("HardwareSensorStatus", $"No matching available sensors for {selectedPreset} preset.");
                    return;
                }
                if (await AddSourcePageAsync(added =>
                {
                    added.Name = page.Name;
                    added.Line1Template = page.Line1Template;
                    added.Line2Template = page.Line2Template;
                    added.DurationSeconds = 5;
                    added.Line1OverflowMode = "PingPong";
                    added.Line2OverflowMode = "PingPong";
                }))
                    SetVirtualStatus($"Created {selectedPreset} display preset.");
            };
        }
        if (_startBackgroundSources)
        {
            RefreshHardwareSensors();
            _hardwareRefreshTimer.Start();
        }
    }

    private void SetHardwareFanAssignmentEnabled(bool enabled)
    {
        for (var index = 0; index < 4; index++)
        {
            var name = index == 0 ? "HardwareUseForFan" : "HardwareUseForFan" + (index + 1);
            this.FindControl<Button>(name)!.IsEnabled = enabled;
        }
    }

    private void AssignHardwareSensorToFan(int channelIndex)
    {
        if (channelIndex is < 0 or > 3 ||
            this.FindControl<ListBox>("HardwareSensorList")!.SelectedItem is not HardwareSensorEntry { IsAvailable: true } selected)
            return;
        NavigateTo(6);
        this.FindControl<ComboBox>("FanChannelPicker")!.SelectedIndex = channelIndex;
        this.FindControl<TextBox>("FanSensorKey")!.Text = selected.Key;
        SetFanStatus($"Sensor {selected.Key} assigned to Fan {channelIndex + 1} editor. Save configuration to persist it.");
    }

    private void RefreshHardwareSensors()
    {
        try
        {
            var snapshot = _liveFanSensors.Snapshot();
            _hardwareSensors.Clear();
            foreach (var item in snapshot) _hardwareSensors[item.Key] = item.Value;
            _pageSources.UpdateHardware(_hardwareSensors);
            RefreshFanSensorChoices();
            RefreshPageVariables();
            _hardwareSourceError = snapshot.Count == 0 && _liveFanSensors is WindowsHardwareFanSensors windows
                ? windows.Diagnostic
                : null;
            if (_hardwareListLiveUpdates || _selectedSection == 5) FilterHardwareSensors();
        }
        catch (Exception ex)
        {
            _hardwareSensors.Clear();
            _pageSources.UpdateHardware(_hardwareSensors);
            RefreshFanSensorChoices();
            RefreshPageVariables();
            _hardwareSourceError = ex.Message;
            if (_hardwareListLiveUpdates || _selectedSection == 5) FilterHardwareSensors();
        }
    }

    // Filter by the stable option index, not translated ComboBoxItem.Content.
    // Language changes must never alter which physical sensor types are shown.
    internal static string HardwareTypeFilterKey(int selectedIndex) => selectedIndex switch
    {
        1 => "Favorites", 2 => "Unavailable", 3 => "Temperature",
        4 => "Load", 5 => "Fan", 6 => "Clock", 7 => "Voltage",
        8 => "Power", 9 => "Data", 10 => "Throughput", 11 => "Other",
        _ => "All types"
    };

    private void FilterHardwareSensors()
    {
        var search = this.FindControl<TextBox>("HardwareSearch")!.Text?.Trim() ?? "";
        var typeFilter = HardwareTypeFilterKey(this.FindControl<ComboBox>("HardwareTypeFilter")!.SelectedIndex);
        var tracked = HardwareSensorCatalog.ExtractTrackedKeys(
            _pages.Pages.SelectMany(page => new[] { page.Line1Template, page.Line2Template }),
            _fanSettings.Channels.Select(channel => channel.SensorKey));
        var options = HardwareSensorCatalog.Build(_hardwareSensors, _hardwarePreferences, search,
            this.FindControl<CheckBox>("HardwareFavoritesOnly")!.IsChecked == true, typeFilter, tracked)
            .Select(entry => entry with { Language = CurrentUiLanguage() }).ToArray();
        var list = this.FindControl<ListBox>("HardwareSensorList")!;
        var previousKey = _selectedHardwareSensorKey;
        _hardwareListRebinding = true;
        try
        {
            list.ItemsSource = HardwareSensorEntry.GroupForDisplay(options);
            if (previousKey is not null)
                list.SelectedItem = options.FirstOrDefault(option => string.Equals(option.Key, previousKey, StringComparison.OrdinalIgnoreCase));
        }
        finally { _hardwareListRebinding = false; }
        if (list.SelectedItem is null)
        {
            _selectedHardwareSensorKey = null;
            this.FindControl<TextBox>("HardwareAlias")!.Text = "";
            this.FindControl<CheckBox>("HardwareFavorite")!.IsChecked = false;
            SetUiText("HardwareSelectedSensor", "Select a sensor above.");
        }
        else
        {
            SetUiText("HardwareSelectedSensor", list.SelectedItem is HardwareSensorEntry selected
                ? (UiMessage)$"{selected.SelectionDetails}" : "Select a sensor above.");
        }
        this.FindControl<Button>("HardwareSavePreference")!.IsEnabled = list.SelectedItem is not null;
        SetHardwareFanAssignmentEnabled(list.SelectedItem is HardwareSensorEntry { IsAvailable: true });
        var total = _hardwareSensors.Count(pair => HardwareSensorCatalog.IsSensorKey(pair.Key));
        var unavailable = options.Count(entry => !entry.IsAvailable);
        SetUiText("HardwareSensorStatus", !string.IsNullOrWhiteSpace(_hardwareSourceError)
            ? (UiMessage)$"Hardware source error: {_hardwareSourceError}"
            : unavailable > 0
                ? (UiMessage)$"{options.Length} visible sensor(s) / {total} total values · {unavailable} unavailable configured sensor(s)"
                : (UiMessage)$"{options.Length} visible sensor(s) / {total} total values");
    }

}
