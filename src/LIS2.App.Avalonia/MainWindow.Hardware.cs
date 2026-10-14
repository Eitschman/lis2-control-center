using System.Globalization;
using Avalonia.Controls;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly List<KeyValuePair<string, object?>> _hardwareSensors = new();

    private void InitializeHardwareBrowser()
    {
        this.FindControl<Button>("HardwareRefresh")!.Click += (_, _) => RefreshHardwareSensors();
        this.FindControl<TextBox>("HardwareSearch")!.TextChanged += (_, _) => FilterHardwareSensors();
        this.FindControl<ListBox>("HardwareSensorList")!.SelectionChanged += (_, _) =>
            this.FindControl<Button>("HardwareUseForFan")!.IsEnabled =
                this.FindControl<ListBox>("HardwareSensorList")!.SelectedItem is HardwareSensorOption;
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
            .Where(item => item.Key.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Select(item => new HardwareSensorOption(item.Key, item.Value))
            .ToList();
        var list = this.FindControl<ListBox>("HardwareSensorList")!;
        list.ItemsSource = options;
        this.FindControl<Button>("HardwareUseForFan")!.IsEnabled = false;
        this.FindControl<TextBlock>("HardwareSensorStatus")!.Text =
            $"{options.Count} of {_hardwareSensors.Count} temperature sensor keys shown. Select one to assign it.";
    }

    private sealed record HardwareSensorOption(string Key, object? Value)
    {
        public override string ToString() =>
            $"{Key}  =  {Convert.ToString(Value, CultureInfo.InvariantCulture)} °C";
    }
}
