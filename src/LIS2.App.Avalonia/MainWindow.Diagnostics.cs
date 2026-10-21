using Avalonia.Controls;
using Avalonia.Platform.Storage;
using LIS2.Core;
using System.Globalization;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private void InitializeDiagnostics()
    {
        this.FindControl<Button>("DiagnosticsRefresh")!.Click += (_, _) => RefreshDiagnostics();
        this.FindControl<Button>("DiagnosticsExport")!.Click += async (_, _) =>
        {
            try
            {
                RefreshDiagnostics();
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = UiText("Export diagnostics report"),
                    SuggestedFileName = "lis2-diagnostics.txt",
                    FileTypeChoices = new[] { new FilePickerFileType(UiText("Text report")) { Patterns = new[] { "*.txt" } } }
                });
                if (file is null) return;
                await using var stream = await file.OpenWriteAsync();
                stream.SetLength(0);
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(BuildDiagnosticsReport());
            }
            catch (Exception ex)
            {
                SetVirtualStatus($"Diagnostics export failed: {(UiMessage)ex.Message}");
            }
        };
        RefreshDiagnostics();
    }

    internal string BuildDiagnosticsReport()
    {
        RefreshDiagnostics();
        return string.Join(Environment.NewLine + Environment.NewLine, new[]
        {
            UiText("Diagnostics Snapshot") + Environment.NewLine + (this.FindControl<TextBox>("DiagnosticsSnapshot")!.Text ?? ""),
            UiText("Virtual LIS2 state") + Environment.NewLine + (this.FindControl<TextBox>("DiagnosticsVirtualState")!.Text ?? ""),
            UiText("Decoded command history") + Environment.NewLine + (this.FindControl<TextBox>("DiagnosticsCommandHistory")!.Text ?? ""),
            UiText("Protocol Log") + Environment.NewLine + (this.FindControl<TextBox>("DiagnosticsProtocolLog")!.Text ?? "")
        });
    }

    private void RefreshDiagnostics()
    {
        var serial = _serialDisplay?.IsConnected == true;
        var virtualConnected = _virtualDevice.IsConnected;
        var playback = _virtualSequence?.IsRunning == true;
        var fanMonitor = _fanMonitorTask is { IsCompleted: false };
        var sensorRefresh = _hardwareRefreshTimer.IsEnabled;
        var fanPreviewConfigured = _fanSettings.Channels.Count(channel => !string.IsNullOrWhiteSpace(channel.SensorKey));
        var summary = RuntimeDiagnosticsSnapshot.Format(
            OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsLinux() ? "Linux" : "Other",
            virtualConnected, serial, playback, fanMonitor, _virtualDevice.CommandCount,
            _serialBusy, sensorRefresh, fanPreviewConfigured, UiText);
        this.FindControl<TextBox>("DiagnosticsSnapshot")!.Text = summary;
        var state = _virtualDevice.State;
        var stateLines = new List<string>
        {
            UiFormat($"Line 1: [{state.Line1}]"),
            UiFormat($"Line 2: [{state.Line2}]"),
            UiFormat($"Brightness: {state.Brightness}"),
            UiFormat($"Fans: {state.Fan1}% / {state.Fan2}% / {state.Fan3}% / {state.Fan4}%"),
            UiFormat($"Rejected commands: {state.RejectedCommandCount}"),
            UiFormat($"Last protocol error: {state.LastProtocolError ?? "-"}"),
            ""
        };
        for (var slot = 0; slot < state.CustomCharacters.Length; slot++)
            stateLines.Add(UiFormat($"Glyph {slot + 1}: {string.Join(" ", state.CustomCharacters[slot].Select(value => value.ToString("X2", CultureInfo.InvariantCulture)))}"));
        this.FindControl<TextBox>("DiagnosticsVirtualState")!.Text = string.Join(Environment.NewLine, stateLines);

        var history = _virtualDevice.CommandHistory;
        var firstHistoryNumber = Math.Max(1, history.Count - 199);
        this.FindControl<TextBox>("DiagnosticsCommandHistory")!.Text = string.Join(
            Environment.NewLine,
            history.TakeLast(200).Select((data, index) =>
                $"{firstHistoryNumber + index,4}: " +
                string.Join(" ", data.Select(value => value.ToString("X2", CultureInfo.InvariantCulture))) +
                Environment.NewLine + "      " + Lis2Protocol.DescribeCommand(data)));
        this.FindControl<TextBox>("DiagnosticsProtocolLog")!.Text =
            UiText("Application") + Environment.NewLine + _applicationJournal.Snapshot() + Environment.NewLine +
            UiText("Virtual transport") + Environment.NewLine + _virtualDevice.Journal.Snapshot() + Environment.NewLine +
            string.Join(Environment.NewLine, _serialJournals.Select((journal, index) =>
                UiFormat($"Serial session {index + 1}") + Environment.NewLine + journal.Snapshot()));
    }
}
