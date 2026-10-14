using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private void InitializeDiagnostics()
    {
        this.FindControl<Button>("DiagnosticsRefresh")!.Click += (_, _) => RefreshDiagnostics();
        this.FindControl<Button>("DiagnosticsExport")!.Click += async (_, _) =>
        {
            RefreshDiagnostics();
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export diagnostics report",
                SuggestedFileName = "lis2-diagnostics.txt",
                FileTypeChoices = new[] { new FilePickerFileType("Text report") { Patterns = new[] { "*.txt" } } }
            });
            if (file is null) return;
            try
            {
                await using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(BuildDiagnosticsReport());
            }
            catch (Exception ex)
            {
                SetVirtualStatus("Diagnostics export failed: " + ex.Message);
            }
        };
        RefreshDiagnostics();
    }

    private string BuildDiagnosticsReport() =>
        string.Join(Environment.NewLine + Environment.NewLine, new[]
        {
            "Diagnostics Snapshot" + Environment.NewLine + (this.FindControl<TextBox>("DiagnosticsSnapshot")!.Text ?? ""),
            "Virtual LIS2 state" + Environment.NewLine + (this.FindControl<TextBox>("DiagnosticsVirtualState")!.Text ?? ""),
            "Decoded command history" + Environment.NewLine + (this.FindControl<TextBox>("DiagnosticsCommandHistory")!.Text ?? ""),
            "Protocol Log" + Environment.NewLine + (this.FindControl<TextBox>("DiagnosticsProtocolLog")!.Text ?? "")
        });

    private void RefreshDiagnostics()
    {
        var serial = _serialDisplay is not null;
        var virtualConnected = _virtualDevice.IsConnected;
        var playback = _virtualSequence?.IsRunning == true;
        var fanMonitor = _fanMonitorTask is { IsCompleted: false };
        var sensorRefresh = _hardwareRefreshTimer.IsEnabled;
        var fanPreviewConfigured = _fanSettings.Channels.Count(channel => !string.IsNullOrWhiteSpace(channel.SensorKey));
        var summary = RuntimeDiagnosticsSnapshot.Format(
            OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsLinux() ? "Linux" : "Other",
            virtualConnected, serial, playback, fanMonitor, _virtualDevice.CommandCount,
            _serialBusy, sensorRefresh, fanPreviewConfigured);
        this.FindControl<TextBox>("DiagnosticsSnapshot")!.Text = summary;
        this.FindControl<TextBox>("DiagnosticsVirtualState")!.Text = _virtualDevice.State.ToString() ?? "";
        this.FindControl<TextBox>("DiagnosticsCommandHistory")!.Text =
            "Decoded command history is not yet available in the Avalonia runtime.";
        this.FindControl<TextBox>("DiagnosticsProtocolLog")!.Text =
            "Protocol log capture is not yet available in the Avalonia runtime.";
    }
}
