using System.Globalization;
using Avalonia.Controls;
using Avalonia.Threading;
using LIS2.Core;
using LIS2.Runtime;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly SimulatedFanSensors _fanPreviewSensors = new();
    private readonly SimulatedFanOutput _fanPreviewSink = new();
    private readonly IFanSensorSource _liveFanSensors;
    private AuthorizedFanRuntimeSession? _virtualFanSession;
    private VirtualLis2Transport? _virtualFanTransport;
    private CancellationTokenSource? _fanMonitorStop;
    private Task? _fanMonitorTask;
    private FanRuntime? _fanMonitorRuntime;
    private bool _fanEditorLoading;
    private int _fanEditingChannelIndex;
    private sealed record FanSensorChoice(string Key, string Label)
    {
        public override string ToString() => Label;
    }

    /// <summary>Die Auswahl ergänzt freie Schlüssel; ausgefallene konfigurierte Sensoren bleiben editierbar.</summary>
    private void RefreshFanSensorChoices()
    {
        var key = this.FindControl<TextBox>("FanSensorKey")!.Text;
        var entries = HardwareSensorCatalog.Build(_hardwareSensors, _hardwarePreferences,
            trackedKeys: string.IsNullOrWhiteSpace(key) ? [] : new[] { key });
        this.FindControl<ComboBox>("FanSensorChoices")!.ItemsSource = entries.Select(entry =>
            new FanSensorChoice(entry.Key, (entry with { Language = CurrentUiLanguage() }).ToString())).ToArray();
    }

    private void UpdateFanOutputPreview(IReadOnlyList<int> percentages)
    {
        for (var i = 0; i < Math.Min(4, percentages.Count); i++)
        {
            var percent = Math.Clamp(percentages[i], 0, 100);
            this.FindControl<ProgressBar>($"FanOutputBar{i + 1}")!.Value = percent;
            this.FindControl<TextBlock>($"FanOutputText{i + 1}")!.Text = percent + " %";
        }
    }

    private void RefreshFanCurvePreview()
    {
        var target = this.FindControl<global::Avalonia.Controls.Shapes.Path>("FanCurvePreviewPath");
        if (target is null) return;
        var points = new List<(double Temperature, double Percent)>();
        foreach (var line in (this.FindControl<TextBox>("FanCurve")?.Text ?? "").Split('\n'))
        {
            var fields = line.Trim().Split('=');
            if (fields.Length != 2 ||
                !double.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var temp) ||
                !double.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) ||
                !double.IsFinite(temp) || !double.IsFinite(percent) || percent is < 0 or > 100)
                continue;
            points.Add((temp, percent));
        }
        points.Sort((a, b) => a.Temperature.CompareTo(b.Temperature));
        if (points.Count == 0) { target.Data = null; return; }
        var min = Math.Min(0, points[0].Temperature);
        var max = Math.Max(min + 1, points[^1].Temperature);
        var segments = points.Select((point, i) =>
            (i == 0 ? "M " : "L ") +
            ((point.Temperature - min) / (max - min) * 298).ToString("0.##", CultureInfo.InvariantCulture) + "," +
            (110 - point.Percent * 1.1).ToString("0.##", CultureInfo.InvariantCulture));
        target.Data = global::Avalonia.Media.Geometry.Parse(string.Join(" ", segments));
    }

    private void InitializeFanEditor()
    {
        InitializeFanHistory();
        InitializeCurvePoints();
        SetUiText("FanSensorSource", OperatingSystem.IsLinux() ? "Linux system sensors" : "Windows hardware sensors");
        this.FindControl<ComboBox>("FanSensorChoices")!.SelectionChanged += (_, _) =>
        {
            if (this.FindControl<ComboBox>("FanSensorChoices")!.SelectedItem is FanSensorChoice selected)
                this.FindControl<TextBox>("FanSensorKey")!.Text = selected.Key;
        };
        this.FindControl<TextBox>("FanCurve")!.TextChanged += (_, _) => { RefreshFanCurvePreview(); RefreshCurvePoints(); };
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
                SetFanStatus($"Unsaved changes: {(UiMessage)ex.Message}");
            }
        };
        this.FindControl<Button>("FanSave")!.Click += async (_, _) =>
        {
            try
            {
                SaveFanEditor();
                await SavePortableWorkspaceAsync(true);
                await UpdateRunningFanConfigurationAsync();
                SetFanStatus("Fan settings saved. Physical fan output requires separate authorization.");
            }
            catch (Exception ex) { SetFanStatus($"Invalid fan settings: {(UiMessage)ex.Message}"); }
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
                UpdateFanOutputPreview(state.Channels.Select(channel => channel.OutputPercent).ToArray());
                SetFanStatus($"DRY RUN: {FormatFanOutputs(state.Channels)}");
            }
            catch (Exception ex) { SetFanStatus($"Simulation failed: {(UiMessage)ex.Message}"); }
        };
        this.FindControl<Button>("FanReadSensors")!.Click += (_, _) =>
        {
            try
            {
                var values = _liveFanSensors.Snapshot();
                SetUiText("FanSensorStatus", values.Count == 0
                    ? "No readable temperature sensors found. Missing keys trigger fail-safe."
                    : string.Join(Environment.NewLine, values.OrderBy(x => x.Key).Select(x =>
                        x.Key + " = " + LIS2.Display.SourceValueFormatter.FormatNumeric(x.Value) + " C")));
            }
            catch (Exception ex) { SetUiText("FanSensorStatus", $"Sensor read failed: {(UiMessage)ex.Message}"); }
        };
        this.FindControl<Button>("FanLivePreview")!.Click += async (_, _) =>
        {
            try
            {
                SaveFanEditor();
                await using var runtime = new FanRuntime(_liveFanSensors, _fanPreviewSink, _fanSettings.ToRuntimeChannels());
                var state = await runtime.TickAsync();
                UpdateFanOutputPreview(state.Channels.Select(channel => channel.OutputPercent).ToArray());
                SetFanStatus($"LIVE SENSOR DRY RUN (no hardware output): {FormatFanOutputs(state.Channels)}");
            }
            catch (Exception ex) { SetFanStatus($"Live sensor preview failed: {(UiMessage)ex.Message}"); }
        };
        this.FindControl<Button>("FanMonitorStart")!.Click += async (_, _) =>
        {
            if (_fanMonitorTask is not null) return;
            try
            {
                SaveFanEditor();
                var channels = _fanSettings.ToRuntimeChannels();
                var stop = new CancellationTokenSource();
                _fanMonitorStop = stop;
                this.FindControl<Button>("FanMonitorStart")!.IsEnabled = false;
                this.FindControl<Button>("FanMonitorStop")!.IsEnabled = true;
                _fanMonitorTask = RunFanMonitorAsync(channels, stop.Token);
                await Task.Yield();
            }
            catch (Exception ex) { SetFanStatus($"Monitor start failed: {(UiMessage)ex.Message}"); }
        };
        this.FindControl<Button>("FanMonitorStop")!.Click += async (_, _) => await StopFanMonitorAsync();
        this.FindControl<CheckBox>("FanHardwareConsent")!.IsCheckedChanged += (_, _) => UpdateHardwareFanControls();
        this.FindControl<Button>("FanHardwareStart")!.Click += async (_, _) =>
        {
            var session = _serialDisplay;
            if (_serialBusy || _settingsTransportBusy || _dashboardFansBusy || session is null || !session.IsConnected ||
                this.FindControl<CheckBox>("FanHardwareConsent")!.IsChecked != true) return;
            try
            {
                SaveFanEditor();
                // Consent is per connection, never persisted. No automatic hardware startup.
                this.FindControl<CheckBox>("FanHardwareConsent")!.IsChecked = false;
                _serialBusy = true;
                UpdateSerialControls();
                await session.StartFansAsync(_liveFanSensors, _fanSettings.ToRuntimeChannels());
                SetFanStatus("LIVE SERIAL FAN CONTROL ACTIVE: four channels, 2-second loop, fail-safe enabled.");
            }
            catch (Exception ex) { SetFanStatus($"Hardware fan start failed: {(UiMessage)ex.Message}"); }
            finally { _serialBusy = false; UpdateSerialControls(); }
        };
        this.FindControl<Button>("FanHardwareStop")!.Click += async (_, _) =>
        {
            var session = _serialDisplay;
            if (_serialBusy || session is null) return;
            try
            {
                _serialBusy = true;
                UpdateSerialControls();
                await session.StopFansAsync();
                SetFanStatus("Hardware fan control stopped; safety frame requested; display stays connected.");
            }
            catch (Exception ex) { SetFanStatus($"Hardware fan stop failed: {(UiMessage)ex.Message}"); }
            finally { _serialBusy = false; UpdateSerialControls(); }
        };
        this.FindControl<Button>("FanVirtualStart")!.Click += async (_, _) =>
        {
            if (_virtualFanSession is not null) return;
            AuthorizedFanRuntimeSession? pending = null;
            try
            {
                SaveFanEditor();
                var temperature = double.Parse(this.FindControl<TextBox>("FanSimulatedTemperature")!.Text ?? "",
                    CultureInfo.InvariantCulture);
                if (!double.IsFinite(temperature)) throw new FormatException("Simulated temperature must be finite.");
                foreach (var channel in _fanSettings.Channels)
                    if (!string.IsNullOrWhiteSpace(channel.SensorKey))
                        _fanPreviewSensors.Set(channel.SensorKey, temperature);
                var transport = new VirtualLis2Transport();
                pending = new AuthorizedFanRuntimeSession(transport, _fanPreviewSensors,
                    _fanSettings.ToRuntimeChannels());
                pending.Faulted += ex => Dispatcher.UIThread.Post(() =>
                    SetFanStatus($"VIRTUAL FAN LOOP FAILED: {(UiMessage)ex.Message}"));
                await pending.StartAsync(new[] { 100, 100, 100, 100 });
                _virtualFanTransport = transport;
                _virtualFanSession = pending;
                pending = null;
                this.FindControl<Button>("FanVirtualStart")!.IsEnabled = false;
                this.FindControl<Button>("FanVirtualStop")!.IsEnabled = true;
                SetFanStatus($"VIRTUAL LIS2 FAN CONTROL: safe frame 100% sent first; four-channel regulation active; no physical port opened.");
            }
            catch (Exception ex) { SetFanStatus($"Virtual fan startup failed: {(UiMessage)ex.Message}"); }
            finally { if (pending is not null) await pending.DisposeAsync(); }
        };
        this.FindControl<Button>("FanVirtualStop")!.Click += async (_, _) =>
        {
            try { await StopVirtualFanSessionAsync(); }
            catch (Exception ex) { SetFanStatus($"Virtual fan shutdown failed: {(UiMessage)ex.Message}"); }
        };
        RefreshFanEditor();
    }

    private void UpdateHardwareFanControls()
    {
        var start = this.FindControl<Button>("FanHardwareStart");
        var stop = this.FindControl<Button>("FanHardwareStop");
        if (start is null || stop is null) return;
        var session = _serialDisplay;
        var connected = session?.IsConnected == true;
        var running = session?.FansRunning == true;
        start.IsEnabled = connected && !running && session?.FansBusy != true && !_serialBusy && !_settingsTransportBusy && !_dashboardFansBusy &&
            this.FindControl<CheckBox>("FanHardwareConsent")?.IsChecked == true;
        stop.IsEnabled = connected && running && session?.FansBusy != true && !_serialBusy;
        UpdateDashboardFanControls();
        if (session?.FanFault is { } fault)
            SetFanStatus($"HARDWARE FAN CONTROL FAILED; safety restoration attempted: {(UiMessage)fault.Message}");
    }

    private async Task StopVirtualFanSessionAsync()
    {
        var session = _virtualFanSession;
        if (session is null) return;
        // Retain ownership until drain and disposal complete; never start a second loop mid-shutdown.
        try
        {
            await session.DisposeAsync();
            var writes = _virtualFanTransport?.Writes.Count ?? 0;
            SetFanStatus($"Virtual fan session stopped; {writes} simulated protocol writes; safety frame restored.");
        }
        finally
        {
            _virtualFanSession = null;
            _virtualFanTransport = null;
            this.FindControl<Button>("FanVirtualStart")!.IsEnabled = true;
            this.FindControl<Button>("FanVirtualStop")!.IsEnabled = false;
        }
    }

    private async Task RunFanMonitorAsync(IReadOnlyList<LIS2.Fans.FanChannelConfiguration> channels, CancellationToken token)
    {
        try
        {
            await using var runtime = new FanRuntime(_liveFanSensors, new SimulatedFanOutput(), channels);
            _fanMonitorRuntime = runtime;
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
            do
            {
                var state = await runtime.TickAsync(token);
                await Dispatcher.UIThread.InvokeAsync(() => SetFanStatus($"CONTINUOUS DRY RUN (hardware disabled): {FormatFanOutputs(state.Channels)}"));
            } while (await timer.WaitForNextTickAsync(token));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => SetFanStatus($"Fan monitor failed: {(UiMessage)ex.Message}"));
        }
        finally
        {
            _fanMonitorRuntime = null;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.FindControl<Button>("FanMonitorStart")!.IsEnabled = true;
                this.FindControl<Button>("FanMonitorStop")!.IsEnabled = false;
            });
        }
    }

    /// <summary>Gespeicherte Kanäle gelten ab dem nächsten Regeltakt; bestehende Freigaben werden nicht erweitert.</summary>
    private async Task UpdateRunningFanConfigurationAsync()
    {
        var channels = _fanSettings.ToRuntimeChannels();
        if (_serialDisplay is { } serial) await serial.ConfigureFansAsync(channels);
        if (_virtualFanSession is { } virtualSession) await virtualSession.ConfigureAsync(channels);
        if (_fanMonitorRuntime is { } monitor) await monitor.ConfigureAsync(channels);
    }

    private async Task StopFanMonitorAsync()
    {
        var stop = _fanMonitorStop;
        var task = _fanMonitorTask;
        stop?.Cancel();
        try { if (task is not null) await task; }
        finally
        {
            stop?.Dispose();
            if (ReferenceEquals(_fanMonitorStop, stop))
            {
                _fanMonitorStop = null;
                _fanMonitorTask = null;
            }
        }
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
            RefreshFanSensorChoices();
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
        RefreshFanCurvePreview();
    }

    private void SaveFanEditor()
    {
        var index = _fanEditingChannelIndex;
        int ReadInt(string name) => int.Parse(this.FindControl<TextBox>(name)!.Text ?? "", CultureInfo.InvariantCulture);
        var curve = FanCurveEditor.Parse(this.FindControl<TextBox>("FanCurve")!.Text);
        var candidate = new PortableFanChannel
        {
            Name = this.FindControl<TextBox>("FanName")!.Text ?? "",
            Mode = new[] { "Fixed", "Curve", "Follow", "External", "Off" }[Math.Clamp(this.FindControl<ComboBox>("FanModePicker")!.SelectedIndex, 0, 4)],
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

    private static UiMessage FormatFanOutputs(IReadOnlyList<FanChannelState> channels) =>
        UiMessage.Join(" / ", channels.Select((channel, index) =>
        {
            UiMessage safety = channel.SensorValid ? "" : " (fail-safe)";
            return (UiMessage)$"Fan {index + 1}: {channel.OutputPercent}%{safety}";
        }));

    private void SetFanStatus(UiMessage message) => SetUiText("FanPreviewStatus", message);
}
