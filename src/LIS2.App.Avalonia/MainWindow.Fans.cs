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
    private readonly IFanSensorSource _liveFanSensors = OperatingSystem.IsLinux()
        ? new LinuxHwmonFanSensors()
        : new SnapshotFanSensors(() => new Dictionary<string, object?>());
    private AuthorizedFanRuntimeSession? _virtualFanSession;
    private VirtualLis2Transport? _virtualFanTransport;
    private CancellationTokenSource? _fanMonitorStop;
    private Task? _fanMonitorTask;
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
            catch (Exception ex) { SetFanStatus("Monitor start failed: " + ex.Message); }
        };
        this.FindControl<Button>("FanMonitorStop")!.Click += async (_, _) => await StopFanMonitorAsync();
        this.FindControl<CheckBox>("FanHardwareConsent")!.IsCheckedChanged += (_, _) => UpdateHardwareFanControls();
        this.FindControl<Button>("FanHardwareStart")!.Click += async (_, _) =>
        {
            var session = _serialDisplay;
            if (session is null || !session.IsConnected ||
                this.FindControl<CheckBox>("FanHardwareConsent")!.IsChecked != true) return;
            try
            {
                SaveFanEditor();
                // Consent is per connection, never persisted. No automatic hardware startup.
                this.FindControl<CheckBox>("FanHardwareConsent")!.IsChecked = false;
                UpdateHardwareFanControls();
                await session.StartFansAsync(_liveFanSensors, _fanSettings.ToRuntimeChannels());
                SetFanStatus("LIVE SERIAL FAN CONTROL ACTIVE: four channels, 2-second loop, fail-safe enabled.");
            }
            catch (Exception ex) { SetFanStatus("Hardware fan start failed: " + ex.Message); }
            finally { UpdateHardwareFanControls(); }
        };
        this.FindControl<Button>("FanHardwareStop")!.Click += async (_, _) =>
        {
            var session = _serialDisplay;
            if (session is null) return;
            try
            {
                await session.StopFansAsync();
                SetFanStatus("Hardware fan control stopped; safety frame requested; display stays connected.");
            }
            catch (Exception ex) { SetFanStatus("Hardware fan stop failed: " + ex.Message); }
            finally { UpdateHardwareFanControls(); }
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
                    SetFanStatus("VIRTUAL FAN LOOP FAILED: " + ex.Message));
                await pending.StartAsync(new[] { 100, 100, 100, 100 });
                _virtualFanTransport = transport;
                _virtualFanSession = pending;
                pending = null;
                this.FindControl<Button>("FanVirtualStart")!.IsEnabled = false;
                this.FindControl<Button>("FanVirtualStop")!.IsEnabled = true;
                SetFanStatus("VIRTUAL LIS2 FAN CONTROL: safe frame 100% sent first; " +
                    "four-channel regulation active; no physical port opened.");
            }
            catch (Exception ex) { SetFanStatus("Virtual fan startup failed: " + ex.Message); }
            finally { if (pending is not null) await pending.DisposeAsync(); }
        };
        this.FindControl<Button>("FanVirtualStop")!.Click += async (_, _) =>
        {
            try { await StopVirtualFanSessionAsync(); }
            catch (Exception ex) { SetFanStatus("Virtual fan shutdown failed: " + ex.Message); }
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
        start.IsEnabled = connected && !running && !_serialBusy &&
            this.FindControl<CheckBox>("FanHardwareConsent")?.IsChecked == true;
        stop.IsEnabled = connected && running && !_serialBusy;
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
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
            do
            {
                var state = await runtime.TickAsync(token);
                await Dispatcher.UIThread.InvokeAsync(() => SetFanStatus(
                    "CONTINUOUS DRY RUN (hardware disabled): " + string.Join(" / ",
                    state.Channels.Select((c, i) => $"Fan {i + 1}: {c.OutputPercent}%{(c.SensorValid ? "" : " (fail-safe)")}"))));
            } while (await timer.WaitForNextTickAsync(token));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => SetFanStatus("Fan monitor failed: " + ex.Message));
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.FindControl<Button>("FanMonitorStart")!.IsEnabled = true;
                this.FindControl<Button>("FanMonitorStop")!.IsEnabled = false;
            });
        }
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
