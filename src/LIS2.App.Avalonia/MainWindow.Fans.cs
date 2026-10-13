using System.Globalization;
using Avalonia.Controls;
using LIS2.Runtime;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly SimulatedFanSensors _fanPreviewSensors = new();
    private readonly SimulatedFanOutput _fanPreviewSink = new();
    private readonly IFanSensorSource _liveFanSensors = OperatingSystem.IsLinux()
        ? new LinuxHwmonFanSensors()
        : new SnapshotFanSensors(() => new Dictionary<string, object?>());
    private bool _fanEditorLoading;
    private int _fanEditingChannelIndex;

    private void InitializeFanEditor()
    {
        this.FindControl<ComboBox>("FanChannelPicker")!.SelectionChanged += (_, _) =>
        {
            if (_fanEditorLoading) return;
            // Do not silently discard edits when switching channels.
            var requested = this.FindControl<ComboBox>("FanChannelPicker")!.SelectedIndex;
            try
            {
                SaveFanEditor();
                _fanEditingChannelIndex = Math.Clamp(requested, 0, 3);
                RefreshFanEditor();
            }
            catch (Exception ex)
            {
                _fanEditorLoading = true;
                try { this.FindControl<ComboBox>("FanChannelPicker")!.SelectedIndex = _fanEditingChannelIndex; }
                finally { _fanEditorLoading = false; }
                SetFanStatus($"Unsaved changes: {ex.Message}");
            }
        };
        this.FindControl<Button>("FanSave")!.Click += async (_, _) =>
        {
            try
            {
                SaveFanEditor();
                await SavePortableWorkspaceAsync(true);
                SetFanStatus("Fan settings saved. Hardware output remains disabled.");
            }
            catch (Exception ex) { SetFanStatus($"Invalid fan settings: {ex.Message}"); }
        };
        this.FindControl<Button>("FanPreview")!.Click += async (_, _) =>
        {
            try
            {
                SaveFanEditor();
                var temperature = double.Parse(this.FindControl<TextBox>("FanSimulatedTemperature")!.Text ?? "",
                    CultureInfo.InvariantCulture);
                if (!double.IsFinite(temperature)) throw new FormatException("Sensor temperature must be finite.");
                foreach (var channel in _fanSettings.Channels)
                    if (!string.IsNullOrWhiteSpace(channel.SensorKey))
                        _fanPreviewSensors.Set(channel.SensorKey, temperature);
                await using var runtime = new FanRuntime(_fanPreviewSensors, _fanPreviewSink, _fanSettings.ToRuntimeChannels());
                var state = await runtime.TickAsync();
                SetFanStatus("DRY RUN: " + string.Join(" / ", state.Channels.Select((c, i) =>
                    $"Fan {i + 1}: {c.OutputPercent}%{(c.SensorValid ? "" : " (fail-safe)") }")));
            }
            catch (Exception ex) { SetFanStatus($"Simulation failed: {ex.Message}"); }
        };
        this.FindControl<Button>("FanReadSensors")!.Click += (_, _) =>
        {
            try
            {
                var values = _liveFanSensors.Snapshot();
                this.FindControl<TextBlock>("FanSensorStatus")!.Text = values.Count == 0
                    ? "No readable hwmon temperature sensors found. Missing keys trigger fail-safe."
                    : string.Join(Environment.NewLine, values.OrderBy(x => x.Key).Select(x =>
                        x.Key + " = " + Convert.ToString(x.Value, CultureInfo.InvariantCulture) + " C"));
            }
            catch (Exception ex) { this.FindControl<TextBlock>("FanSensorStatus")!.Text = "Sensor read failed: " + ex.Message; }
        };
        this.FindControl<Button>("FanLivePreview")!.Click += async (_, _) =>
        {
            try
            {
                SaveFanEditor();
                await using var runtime = new FanRuntime(_liveFanSensors, _fanPreviewSink, _fanSettings.ToRuntimeChannels());
                var state = await runtime.TickAsync();
                SetFanStatus("LIVE SENSOR DRY RUN (no hardware output): " +
                    string.Join(" / ", state.Channels.Select((c, i) =>
                        $"Fan {i + 1}: {c.OutputPercent}%{(c.SensorValid ? "" : " (fail-safe)")}")));
            }
            catch (Exception ex) { SetFanStatus("Live sensor preview failed: " + ex.Message); }
        };
        RefreshFanEditor();
    }

    private void RefreshFanEditor()
    {
        var picker = this.FindControl<ComboBox>("FanChannelPicker");
        if (picker is null) return;
        _fanEditorLoading = true;
        try
        {
            var channel = _fanSettings.Channels[_fanEditingChannelIndex];
            this.FindControl<CheckBox>("FanAutomatic")!.IsChecked = _fanSettings.AutomaticControlEnabled;
            this.FindControl<ComboBox>("FanModePicker")!.SelectedIndex =
                Array.IndexOf(new[] { "Fixed", "Curve", "Follow", "External", "Off" }, channel.Mode);
            this.FindControl<TextBox>("FanName")!.Text = channel.Name;
            this.FindControl<TextBox>("FanSensorKey")!.Text = channel.SensorKey ?? "";
            this.FindControl<TextBox>("FanFixed")!.Text = channel.FixedPercent.ToString(CultureInfo.InvariantCulture);
            this.FindControl<TextBox>("FanMinimum")!.Text = channel.MinimumPercent.ToString(CultureInfo.InvariantCulture);
            this.FindControl<TextBox>("FanMaximum")!.Text = channel.MaximumPercent.ToString(CultureInfo.InvariantCulture);
            this.FindControl<TextBox>("FanFailSafe")!.Text = channel.FailSafePercent.ToString(CultureInfo.InvariantCulture);
            this.FindControl<TextBox>("FanHysteresis")!.Text = channel.HysteresisDegrees.ToString(CultureInfo.InvariantCulture);
            this.FindControl<CheckBox>("FanAllowStop")!.IsChecked = channel.AllowStop;
            this.FindControl<TextBox>("FanCurve")!.Text = string.Join(Environment.NewLine,
                channel.Curve.Select(p => $"{p.Temperature.ToString(CultureInfo.InvariantCulture)}={p.OutputPercent}"));
        }
        finally { _fanEditorLoading = false; }
    }

    private void SaveFanEditor()
    {
        var index = _fanEditingChannelIndex;
        int ReadInt(string name) => int.Parse(this.FindControl<TextBox>(name)!.Text ?? "", CultureInfo.InvariantCulture);
        var curve = new List<PortableFanCurvePoint>();
        foreach (var line in (this.FindControl<TextBox>("FanCurve")!.Text ?? "").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Trim().Split('=');
            if (parts.Length != 2) throw new FormatException("Curve point must be temperature=percent.");
            curve.Add(new PortableFanCurvePoint(double.Parse(parts[0], CultureInfo.InvariantCulture),
                int.Parse(parts[1], CultureInfo.InvariantCulture)));
        }
        var candidate = new PortableFanChannel
        {
            Name = this.FindControl<TextBox>("FanName")!.Text ?? "",
            Mode = (this.FindControl<ComboBox>("FanModePicker")!.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Fixed",
            SensorKey = this.FindControl<TextBox>("FanSensorKey")!.Text,
            FixedPercent = ReadInt("FanFixed"), MinimumPercent = ReadInt("FanMinimum"),
            MaximumPercent = ReadInt("FanMaximum"), FailSafePercent = ReadInt("FanFailSafe"),
            HysteresisDegrees = double.Parse(this.FindControl<TextBox>("FanHysteresis")!.Text ?? "", CultureInfo.InvariantCulture),
            AllowStop = this.FindControl<CheckBox>("FanAllowStop")!.IsChecked == true, Curve = curve
        };
        if (candidate.MinimumPercent is < 0 or > 100 ||
            candidate.MaximumPercent < candidate.MinimumPercent || candidate.MaximumPercent > 100 ||
            candidate.FixedPercent is < 0 or > 100 ||
            candidate.FailSafePercent < candidate.MinimumPercent || candidate.FailSafePercent > candidate.MaximumPercent ||
            !double.IsFinite(candidate.HysteresisDegrees) || candidate.HysteresisDegrees is < 0 or > 50 ||
            curve.Any(p => !double.IsFinite(p.Temperature) || p.OutputPercent is < 0 or > 100))
            throw new ArgumentException("Output, fail-safe or curve outside allowed bounds.");
        _fanSettings.Channels[index] = PortableFanChannel.Normalize(candidate, index + 1);
        _fanSettings.AutomaticControlEnabled = this.FindControl<CheckBox>("FanAutomatic")!.IsChecked == true;
    }

    private void SetFanStatus(string message) =>
        this.FindControl<TextBlock>("FanPreviewStatus")!.Text = message;
}
