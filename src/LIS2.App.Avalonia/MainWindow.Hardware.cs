using Avalonia.Controls;
using Avalonia.Threading;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly Dictionary<string, object?> _hardwareSensors = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PortableHardwareSensorPreference> _hardwarePreferences = new();
    private bool _hardwareListRebinding;
    private readonly DispatcherTimer _hardwareRefreshTimer = new() { Interval = TimeSpan.FromSeconds(2) };

    private void InitializeHardwareBrowser()
    {
        this.FindControl<Button>("HardwareRefresh")!.Click += (_, _) => RefreshHardwareSensors();
        _hardwareRefreshTimer.Tick += (_, _) => RefreshHardwareSensors();
        this.FindControl<Button>("HardwareAutoRefresh")!.Click += (_, _) =>
        {
            if (_hardwareRefreshTimer.IsEnabled)
                _hardwareRefreshTimer.Stop();
            else
            {
                RefreshHardwareSensors();
                _hardwareRefreshTimer.Start();
            }
            this.FindControl<Button>("HardwareAutoRefresh")!.Content =
                _hardwareRefreshTimer.IsEnabled ? "Stop live updates" : "Start live updates";
        };
        this.FindControl<TextBox>("HardwareSearch")!.TextChanged += (_, _) => FilterHardwareSensors();
        this.FindControl<ComboBox>("HardwareTypeFilter")!.SelectionChanged += (_, _) => FilterHardwareSensors();
        this.FindControl<CheckBox>("HardwareFavoritesOnly")!.IsCheckedChanged += (_, _) => FilterHardwareSensors();
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
            try
            {
                await SavePortableWorkspaceAsync(true);
                FilterHardwareSensors();
            }
            catch (Exception ex)
            {
                preference.Alias = oldAlias;
                preference.IsFavorite = oldFavorite;
                if (created)
                    _hardwarePreferences.Remove(preference);
                this.FindControl<TextBlock>("HardwareSensorStatus")!.Text = "Preference save failed: " + ex.Message;
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
            SetHardwareFanAssignmentEnabled(selected?.IsAvailable == true);
            this.FindControl<Button>("HardwareSavePreference")!.IsEnabled = selected is not null;
            var preference = _hardwarePreferences.FirstOrDefault(p => string.Equals(p.Key, selected?.Key, StringComparison.OrdinalIgnoreCase));
            this.FindControl<TextBox>("HardwareAlias")!.Text = preference?.Alias ?? "";
            this.FindControl<CheckBox>("HardwareFavorite")!.IsChecked = preference?.IsFavorite == true;
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
                    this.FindControl<TextBlock>("HardwareSensorStatus")!.Text =
                        "No matching available sensors for " + selectedPreset + " preset.";
                    return;
                }
                var added = _pages.Add();
                added.Name = page.Name;
                added.Line1Template = page.Line1Template;
                added.Line2Template = page.Line2Template;
                added.DurationSeconds = 5;
                added.Line1OverflowMode = "PingPong";
                added.Line2OverflowMode = "PingPong";
                RefreshPageList(_pages.Pages.Count - 1);
                await SavePortableWorkspaceAsync();
                NavigateTo(2);
                SetVirtualStatus("Created " + selectedPreset + " display preset.");
            };
        }
        RefreshHardwareSensors();
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
            FilterHardwareSensors();
        }
        catch (Exception ex)
        {
            _hardwareSensors.Clear();
            FilterHardwareSensors();
            this.FindControl<TextBlock>("HardwareSensorStatus")!.Text = "Sensor discovery failed: " + ex.Message;
        }
    }

    private void FilterHardwareSensors()
    {
        var search = this.FindControl<TextBox>("HardwareSearch")!.Text?.Trim() ?? "";
        var typeFilter = (this.FindControl<ComboBox>("HardwareTypeFilter")!.SelectedItem as ComboBoxItem)?.Content?.ToString();
        var tracked = HardwareSensorCatalog.ExtractTrackedKeys(
            _pages.Pages.SelectMany(page => new[] { page.Line1Template, page.Line2Template }),
            _fanSettings.Channels.Select(channel => channel.SensorKey));
        var options = HardwareSensorCatalog.Build(_hardwareSensors, _hardwarePreferences, search,
            this.FindControl<CheckBox>("HardwareFavoritesOnly")!.IsChecked == true, typeFilter, tracked);
        var list = this.FindControl<ListBox>("HardwareSensorList")!;
        var previousKey = (list.SelectedItem as HardwareSensorEntry)?.Key;
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
            this.FindControl<TextBox>("HardwareAlias")!.Text = "";
            this.FindControl<CheckBox>("HardwareFavorite")!.IsChecked = false;
        }
        this.FindControl<Button>("HardwareSavePreference")!.IsEnabled = list.SelectedItem is not null;
        SetHardwareFanAssignmentEnabled(list.SelectedItem is HardwareSensorEntry { IsAvailable: true });
        var unavailable = options.Count(item => !item.IsAvailable);
        this.FindControl<TextBlock>("HardwareSensorStatus")!.Text =
            $"{options.Count} sensor keys shown ({unavailable} unavailable). Select an available sensor to assign it.";
    }

}
