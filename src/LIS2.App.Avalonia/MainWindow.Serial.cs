using Avalonia.Controls;
using LIS2.Core;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private SerialDisplaySession? _serialDisplay;
    private bool _serialBusy;

    private void InitializeSerialDisplay()
    {
        this.FindControl<Button>("SerialRefreshPorts")!.Click += (_, _) => RefreshSerialPorts();
        this.FindControl<Button>("SerialConnect")!.Click += async (_, _) => await ConnectSerialAsync();
        this.FindControl<Button>("SerialDisconnect")!.Click += async (_, _) =>
            await RunSerialActionAsync(async () =>
            {
                StopSerialPlayback();
                await _serialPlaybackTimer.StopAndDrainAsync();
                if (_serialSequence is not null) await _serialSequence.StopAndDrainAsync();
                var session = _serialDisplay;
                if (session is not null) await session.DisposeAsync();
                _serialDisplay = null;
                UpdateHardwareFanControls();
                SetSerialStatus("Serial display disconnected.");
            });
        this.FindControl<Button>("SerialSendLines")!.Click += async (_, _) =>
            await RunSerialActionAsync(async () =>
            {
                var session = _serialDisplay ?? throw new InvalidOperationException("Serial display not connected.");
                await session.WriteLineAsync(1, this.FindControl<TextBox>("Line1TextBox")!.Text ?? "");
                await session.WriteLineAsync(2, this.FindControl<TextBox>("Line2TextBox")!.Text ?? "");
                SetSerialStatus("Display lines sent to serial LIS2.");
            });
        this.FindControl<Button>("SerialClear")!.Click += async (_, _) =>
            await RunSerialActionAsync(async () =>
            {
                await (_serialDisplay ?? throw new InvalidOperationException("Serial display not connected.")).ClearAsync();
                SetSerialStatus("Serial LIS2 display cleared.");
            });
        this.FindControl<Button>("SerialSendPage")!.Click += async (_, _) =>
            await RunSerialActionAsync(async () =>
            {
                if (SelectedPageIndex is not int i)
                    throw new InvalidOperationException("Select a page first.");
                if (!SavePageEditor())
                    throw new InvalidOperationException("Page contains invalid settings.");
                var frame = _pages.Preview(i, PagePreviewValues());
                await (_serialDisplay ?? throw new InvalidOperationException("Serial display not connected."))
                    .RenderBothAsync(frame.Line1, frame.Line2);
                SetSerialStatus($"Sent page '{_pages.Get(i).Name}' to serial LIS2.");
            });
        this.FindControl<Button>("SerialSendGlyphs")!.Click += async (_, _) =>
            await RunSerialActionAsync(async () =>
            {
                await (_serialDisplay ?? throw new InvalidOperationException("Serial display not connected."))
                    .ProgramAllGlyphsAsync(_glyphs.Glyphs);
                SetSerialStatus("Programmed all 8 glyph slots on serial LIS2.");
            }, preservePlayback: true);
        this.FindControl<Button>("SerialApplyBrightness")!.Click += async (_, _) =>
            await RunSerialActionAsync(async () =>
            {
                var brightness = this.FindControl<ComboBox>("SerialBrightness")!.SelectedIndex switch
                {
                    0 => Lis2Brightness.Percent100,
                    1 => Lis2Brightness.Percent75,
                    2 => Lis2Brightness.Percent50,
                    3 => Lis2Brightness.Percent25,
                    _ => throw new InvalidOperationException("Select a brightness level.")
                };
                await (_serialDisplay ?? throw new InvalidOperationException("Serial display not connected."))
                    .SetBrightnessAsync(brightness);
                _preferredBrightness = brightness switch
                {
                    Lis2Brightness.Percent100 => 100, Lis2Brightness.Percent75 => 75,
                    Lis2Brightness.Percent50 => 50, _ => 25
                };
                await SavePortableWorkspaceAsync(true);
                SetSerialStatus("Serial LIS2 brightness updated.");
            }, preservePlayback: true);
        InitializeSerialPlayback();
        RefreshSerialPorts();
    }

    private async Task ConnectSerialAsync(string? requestedPort = null)
    {
            if (_serialDisplay is not null || _serialBusy) return;
            var port = requestedPort ?? this.FindControl<ComboBox>("SerialPortPicker")!.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(port))
            {
                SetSerialStatus("Select a serial port first.");
                return;
            }

            _serialBusy = true;
            UpdateSerialControls();
            SerialDisplaySession? session = null;
            try
            {
                session = _serialSessionFactory(port);
                _serialJournals.Add(session.Journal);
                if (_serialJournals.Count > 10) _serialJournals.RemoveAt(0);
                await session.ConnectAsync();
                await ApplyConnectedDisplaySettingsAsync(session);
                _serialDisplay = session;
                session.FanFaulted += ex => global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    SetFanStatus($"HARDWARE FAN CONTROL FAILED; safety restoration attempted: {(UiMessage)ex.Message}");
                    UpdateHardwareFanControls();
                });
                session = null;
                SetSerialStatus($"Connected to {port} (display only; fans disabled).");
                UpdateHardwareFanControls();
            }
            catch (Exception ex) { SetSerialStatus($"Serial connect failed: {(UiMessage)ex.Message}"); }
            finally
            {
                if (session is not null) await session.DisposeAsync();
                _serialBusy = false;
                UpdateSerialControls();
            }
    }

    private void RefreshSerialPorts()
    {
        if (_serialBusy || _serialDisplay is not null) return;
        try
        {
            var picker = this.FindControl<ComboBox>("SerialPortPicker")!;
            var previous = picker.SelectedItem as string;
            var ports = SerialPortDiscovery.GetAvailablePorts();
            picker.ItemsSource = ports;
            picker.SelectedItem = ports.Contains(previous) ? previous : ports.FirstOrDefault();
            SetSerialStatus(ports.Count == 0 ? "No serial ports found." : (UiMessage)$"{ports.Count} serial port(s) available.");
        }
        catch (Exception ex) { SetSerialStatus($"Serial discovery failed: {(UiMessage)ex.Message}"); }
        UpdateSerialControls();
    }

    private async Task RunSerialActionAsync(Func<Task> action, bool preservePlayback = false)
    {
        if (_serialBusy) return;
        if (!preservePlayback && _serialSequence?.IsBusy == true) return;
        // Manual writes, brightness and glyph programming must never race a
        // queued playback write, even when the sequence is already stopped.
        _serialBusy = true;
        var resumeTimer = preservePlayback && _serialPlaybackTimer.IsRunning;
        if (!preservePlayback) StopSerialPlayback();
        UpdateSerialControls();
        try
        {
            await _serialPlaybackTimer.StopAndDrainAsync();
            if (!preservePlayback && _serialSequence is not null)
                await _serialSequence.StopAndDrainAsync();
            if (preservePlayback && _serialSequence is not null)
                await _serialSequence.RunExclusiveAsync(action);
            else await action();
        }
        catch (Exception ex) { StopSerialPlayback(); SetSerialStatus($"Serial display error: {(UiMessage)ex.Message}"); }
        finally
        {
            _serialBusy = false;
            if (resumeTimer && _serialSequence?.IsRunning == true && _serialDisplay?.IsConnected == true) _serialPlaybackTimer.Start();
            UpdatePreview();
            UpdateSerialControls();
        }
    }

    private void UpdateSerialControls()
    {
        var connected = _serialDisplay?.IsConnected == true;
        this.FindControl<ComboBox>("SerialPortPicker")!.IsEnabled = !_serialBusy && _serialDisplay is null;
        this.FindControl<Button>("SerialRefreshPorts")!.IsEnabled = !_serialBusy && _serialDisplay is null;
        this.FindControl<Button>("SerialConnect")!.IsEnabled = !_serialBusy && _serialDisplay is null &&
            this.FindControl<ComboBox>("SerialPortPicker")!.SelectedItem is string;
        this.FindControl<Button>("SerialDisconnect")!.IsEnabled = !_serialBusy && !(_serialSequence?.IsBusy == true) && _serialDisplay is not null;
        this.FindControl<Button>("SerialSendLines")!.IsEnabled = !_serialBusy && connected && !(_serialSequence?.IsRunning == true) && !(_serialSequence?.IsBusy == true);
        this.FindControl<Button>("SerialClear")!.IsEnabled = !_serialBusy && connected && !(_serialSequence?.IsRunning == true) && !(_serialSequence?.IsBusy == true);
        this.FindControl<Button>("SerialPlaybackStart")!.IsEnabled = !_serialBusy && connected && (!(_serialSequence?.IsRunning == true) || _eventStartedSerial) && !(_serialSequence?.IsBusy == true);
        this.FindControl<Button>("SerialPlaybackStop")!.IsEnabled = (_serialSequence?.IsRunning == true);
        UpdateSerialPlaybackStatus();
        UpdateHardwareFanControls();
        UpdateVirtualControls();
        this.FindControl<Button>("SerialSendPage")!.IsEnabled = !_serialBusy && connected && !(_serialSequence?.IsRunning == true) && !(_serialSequence?.IsBusy == true);
        this.FindControl<Button>("SerialSendGlyphs")!.IsEnabled = !_serialBusy && connected && !(_serialSequence?.IsRunning == true) && !(_serialSequence?.IsBusy == true);
        this.FindControl<Button>("SerialApplyBrightness")!.IsEnabled = !_serialBusy && connected && !(_serialSequence?.IsRunning == true) && !(_serialSequence?.IsBusy == true);
    }

    private void SetSerialStatus(UiMessage message)
    {
        SetUiText("SerialStatus", message);
        if (SerialSelected) SetUiText("DeviceActionStatus", message);
    }

    private async Task CloseSerialDisplayAsync()
    {
        StopSerialPlayback();
        await _serialPlaybackTimer.StopAndDrainAsync();
        var session = _serialDisplay;
        if (_serialSequence is not null) await _serialSequence.StopAndDrainAsync();
        if (session is not null) await session.DisposeAsync();
        _serialDisplay = null;
    }
}
