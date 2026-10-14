using System.Globalization;
using Avalonia.Controls;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly List<KeyValuePair<string, object?>> _hardwareSensors = new();
    private readonly List<PortableHardwareSensorPreference> _hardwarePreferences = new();

    private void InitializeHardwareBrowser()
    {
        this.FindControl<Button>("HardwareRefresh")!.Click += (_, _) => RefreshHardwareSensors();
        this.FindControl<TextBox>("HardwareSearch")!.TextChanged += (_, _) => FilterHardwareSensors();
        this.FindControl<CheckBox>("HardwareFavoritesOnly")!.IsCheckedChanged += (_, _) => FilterHardwareSensors();
        this.FindControl<Button>("HardwareSavePreference")!.Click += async (_, _) =>
        {
            if (this.FindControl<ListBox>("HardwareSensorList")!.SelectedItem is not HardwareSensorOption selected) return;
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
            var selected = this.FindControl<ListBox>("HardwareSensorList")!.SelectedItem as HardwareSensorOption;
            this.FindControl<Button>("HardwareUseForFan")!.IsEnabled = selected is not null;
            this.FindControl<Button>("HardwareSavePreference")!.IsEnabled = selected is not null;
            var preference = _hardwarePreferences.FirstOrDefault(p => p.Key == selected?.Key);
            this.FindControl<TextBox>("HardwareAlias")!.Text = preference?.Alias ?? "";
            this.FindControl<CheckBox>("HardwareFavorite")!.IsChecked = preference?.IsFavorite == true;
        };
        this.FindControl<Button>("HardwareUseForFan")!.Click += (_, _) =>
        {
            if (this.FindControl<ListBox>("HardwareSensorList")!.SelectedItem is not HardwareSensorOption selected)
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
            _hardwareSensors.AddRange(snapshot.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase));
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
        var options = _hardwareSensors
            .Where(item =>
            {
                var preference = _hardwarePreferences.FirstOrDefault(p => p.Key == item.Key);
                return (this.FindControl<CheckBox>("HardwareFavoritesOnly")!.IsChecked != true || preference?.IsFavorite == true)
                    && (item.Key.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (preference?.Alias.Contains(search, StringComparison.OrdinalIgnoreCase) == true));
            })
            .Select(item =>
            {
                var preference = _hardwarePreferences.FirstOrDefault(p => p.Key == item.Key);
                return new HardwareSensorOption(item.Key, item.Value, preference?.Alias ?? "", preference?.IsFavorite == true);
            })
            .ToList();
        var list = this.FindControl<ListBox>("HardwareSensorList")!;
        var previousKey = (list.SelectedItem as HardwareSensorOption)?.Key;
        list.ItemsSource = options;
        if (previousKey is not null)
            list.SelectedItem = options.FirstOrDefault(option => option.Key == previousKey);
        this.FindControl<Button>("HardwareUseForFan")!.IsEnabled = false;
        this.FindControl<TextBlock>("HardwareSensorStatus")!.Text =
            $"{options.Count} of {_hardwareSensors.Count} temperature sensor keys shown. Select one to assign it.";
    }

    private sealed record HardwareSensorOption(string Key, object? Value, string Alias, bool Favorite)
    {
        public override string ToString() =>
            $"{(Favorite ? "★ " : "")}{(string.IsNullOrWhiteSpace(Alias) ? Key : Alias + " (" + Key + ")")}  =  {Convert.ToString(Value, CultureInfo.InvariantCulture)} °C";
    }
}
