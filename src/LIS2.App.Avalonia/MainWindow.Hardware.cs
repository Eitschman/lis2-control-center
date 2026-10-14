using Avalonia.Controls;
using Avalonia.Threading;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly Dictionary<string, object?> _hardwareSensors = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PortableHardwareSensorPreference> _hardwarePreferences = new();
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
        this.FindControl<CheckBox>("HardwareFavoritesOnly")!.IsCheckedChanged += (_, _) => FilterHardwareSensors();
        this.FindControl<Button>("HardwareSavePreference")!.Click += async (_, _) =>
        {
            if (this.FindControl<ListBox>("HardwareSensorList")!.SelectedItem is not HardwareSensorEntry selected) return;
            var preference = _hardwarePreferences.FirstOrDefault(p => p.Key == selected.Key);
            if (preference is null)
            {
                preference = new PortableHardwareSensorPreference { Key = selected.Key };
                _hardwarePreferences.Add(preference);
            }
            preference.Alias = this.FindControl<TextBox>("HardwareAlias")!.Text?.Trim() ?? "";
            preference.IsFavorite = this.FindControl<CheckBox>("HardwareFavorite")!.IsChecked == true;
            await SavePortableWorkspaceAsync(true);
            FilterHardwareSensors();
        };
        this.FindControl<ListBox>("HardwareSensorList")!.SelectionChanged += (_, _) =>
        {
            var selected = this.FindControl<ListBox>("HardwareSensorList")!.SelectedItem as HardwareSensorEntry;
            this.FindControl<Button>("HardwareUseForFan")!.IsEnabled = selected?.IsAvailable == true;
            this.FindControl<Button>("HardwareSavePreference")!.IsEnabled = selected is not null;
            var preference = _hardwarePreferences.FirstOrDefault(p => p.Key == selected?.Key);
            this.FindControl<TextBox>("HardwareAlias")!.Text = preference?.Alias ?? "";
            this.FindControl<CheckBox>("HardwareFavorite")!.IsChecked = preference?.IsFavorite == true;
        };
        this.FindControl<Button>("HardwareUseForFan")!.Click += (_, _) =>
        {
            if (this.FindControl<ListBox>("HardwareSensorList")!.SelectedItem is not HardwareSensorEntry selected)
                return;
            this.FindControl<TextBox>("FanSensorKey")!.Text = selected.Key;
            NavigateTo(6);
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
        var options = HardwareSensorCatalog.Build(_hardwareSensors, _hardwarePreferences, search,
            this.FindControl<CheckBox>("HardwareFavoritesOnly")!.IsChecked == true);
        var list = this.FindControl<ListBox>("HardwareSensorList")!;
        var previousKey = (list.SelectedItem as HardwareSensorEntry)?.Key;
        list.ItemsSource = options;
        if (previousKey is not null)
            list.SelectedItem = options.FirstOrDefault(option => option.Key == previousKey);
        this.FindControl<Button>("HardwareUseForFan")!.IsEnabled = list.SelectedItem is HardwareSensorEntry { IsAvailable: true };
        this.FindControl<TextBlock>("HardwareSensorStatus")!.Text =
            $"{options.Count} of {_hardwareSensors.Count} temperature sensor keys shown. Select one to assign it.";
    }

}
