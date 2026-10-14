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
            var selected = this.FindControl<ListBox>("HardwareSensorList")!.SelectedItem as HardwareSensorEntry;
            this.FindControl<Button>("HardwareUseForFan")!.IsEnabled = selected?.IsAvailable == true;
            this.FindControl<Button>("HardwareSavePreference")!.IsEnabled = selected is not null;
            var preference = _hardwarePreferences.FirstOrDefault(p => string.Equals(p.Key, selected?.Key, StringComparison.OrdinalIgnoreCase));
            this.FindControl<TextBox>("HardwareAlias")!.Text = preference?.Alias ?? "";
            this.FindControl<CheckBox>("HardwareFavorite")!.IsChecked = preference?.IsFavorite == true;
        };
        this.FindControl<Button>("HardwareUseForFan")!.Click += (_, _) =>
        {
            if (this.FindControl<ListBox>("HardwareSensorList")!.SelectedItem is not HardwareSensorEntry { IsAvailable: true } selected)
                return;
            NavigateTo(6);
            this.FindControl<TextBox>("FanSensorKey")!.Text = selected.Key;
            SetFanStatus($"Sensor {selected.Key} assigned to the fan editor. Save configuration to persist it.");
        };
        RefreshHardwareSensors();
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
        var options = HardwareSensorCatalog.Build(_hardwareSensors, _hardwarePreferences, search,
            this.FindControl<CheckBox>("HardwareFavoritesOnly")!.IsChecked == true, typeFilter);
        var list = this.FindControl<ListBox>("HardwareSensorList")!;
        var previousKey = (list.SelectedItem as HardwareSensorEntry)?.Key;
        _hardwareListRebinding = true;
        try
        {
            list.ItemsSource = options;
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
        this.FindControl<Button>("HardwareUseForFan")!.IsEnabled = list.SelectedItem is HardwareSensorEntry { IsAvailable: true };
        var unavailable = options.Count(item => !item.IsAvailable);
        this.FindControl<TextBlock>("HardwareSensorStatus")!.Text =
            $"{options.Count} sensor keys shown ({unavailable} unavailable). Select an available sensor to assign it.";
    }

}
