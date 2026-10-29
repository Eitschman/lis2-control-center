using System.Globalization;
using System.Text.Json;
using Avalonia.Controls;
using LIS2.Fans;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly FanHistory _fanHistory = new();
    private readonly string?[] _historyConfigurations = new string?[4];
    private readonly FanHistorySample?[] _historyPrevious = new FanHistorySample?[4];
    private DateTimeOffset _historyNow;
    private bool _historyRefreshing;

    private void InitializeFanHistory()
    {
        this.FindControl<ComboBox>("HistoryChannel")!.SelectionChanged += (_, _) => RefreshFanHistory();
        this.FindControl<ComboBox>("HistoryWindow")!.SelectionChanged += (_, _) => RefreshFanHistory();
        UiLanguageChanged += RefreshFanHistory;
        RefreshFanHistory();
    }

    /// <summary>Nur vorhandene Hardware-Snapshots verwenden; Berechnung erteilt keine Ausgabefreigabe.</summary>
    internal void RecordFanHistory(IReadOnlyDictionary<string, object?> snapshot, DateTimeOffset now)
    {
        _historyNow = now;
        for (var channel = 0; channel < 4; channel++)
        {
            var config = _fanSettings.Channels[channel];
            var rpmKey = _dashboardSettings.FanSpeedKeys.ElementAtOrDefault(channel) ?? "";
            var identity = JsonSerializer.Serialize(config) + "|" + rpmKey;
            if (_historyConfigurations[channel] != identity) _historyPrevious[channel] = null;
            _historyConfigurations[channel] = identity;
            double? Number(string? key) => key is not null && snapshot.TryGetValue(key, out var raw) &&
                TryDashboardNumber(raw, out var number) ? number : null;
            var sensor = Number(config.SensorKey);
            var previous = _historyPrevious[channel];
            // Der Sollwert zeigt die gespeicherte Konfiguration, keine bestätigte physische Ausgabe.
            var needsSensor = config.Mode is "Curve" or "Follow";
            var target = new FanController().CalculateOutput(config.ToRuntime(), sensor, null,
                !needsSensor || sensor.HasValue, previous?.Sensor, previous?.Target is { } output ? (int)output : null);
            var sample = new FanHistorySample(now, sensor, target, Number(rpmKey));
            _fanHistory.Add(channel, identity, sample);
            _historyPrevious[channel] = sample;
        }
        RefreshFanHistory();
    }

    private void RefreshFanHistory()
    {
        if (_historyRefreshing) return;
        if (this.FindControl<FanHistoryPlot>("HistoryPlot") is not { } plot) return;
        // Die Auswahlfläche von ComboBox puffert Inhalte; die übersetzten Zeiträume als Werte neu binden.
        var picker = this.FindControl<ComboBox>("HistoryWindow")!;
        var labels = new[] { "5 min", "15 min", "60 min" }.Select(UiText).ToArray();
        if (!picker.Items.OfType<string>().SequenceEqual(labels))
        {
            var selected = Math.Clamp(picker.SelectedIndex, 0, 2);
            _historyRefreshing = true;
            try { picker.ItemsSource = labels; picker.SelectedIndex = selected; }
            finally { _historyRefreshing = false; }
        }
        var channel = Math.Clamp(this.FindControl<ComboBox>("HistoryChannel")!.SelectedIndex, 0, 3);
        var minutes = this.FindControl<ComboBox>("HistoryWindow")!.SelectedIndex switch { 1 => 15, 2 => 60, _ => 5 };
        var now = _historyNow == default ? DateTimeOffset.UtcNow : _historyNow;
        var samples = _fanHistory.Read(channel, now, TimeSpan.FromMinutes(minutes));
        plot.Update(samples, now, TimeSpan.FromMinutes(minutes));
        var sample = samples.LastOrDefault();
        string Value(double? value, string unit) => value.HasValue ? value.Value.ToString("0.#", CultureInfo.CurrentCulture) + unit : UiText("Unavailable");
        var key = _fanSettings.Channels[channel].SensorKey ?? "";
        var unit = _hardwareSensors.GetValueOrDefault(key + ".Unit")?.ToString() ?? "";
        SetUiText("HistorySensor", Value(sample?.Sensor, unit.Length > 0 ? " " + unit : ""));
        SetUiText("HistoryTarget", Value(sample?.Target, " %"));
        SetUiText("HistoryRpm", string.IsNullOrEmpty(_dashboardSettings.FanSpeedKeys.ElementAtOrDefault(channel)) ? "No speed sensor assigned" : Value(sample?.Rpm, " RPM"));
        SetUiText("HistoryStatus", samples.Length == 0 ? "Waiting for measurements" : "Session history · 60 minutes");
    }
}
