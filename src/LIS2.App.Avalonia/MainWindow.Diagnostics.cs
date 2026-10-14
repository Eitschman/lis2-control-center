using Avalonia.Controls;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private void InitializeDiagnostics()
    {
        this.FindControl<global::Avalonia.Controls.Button>("DiagnosticsRefresh")!.Click += (_, _) => RefreshDiagnostics();
        RefreshDiagnostics();
    }

    private void RefreshDiagnostics()
    {
        var serial = _serialDisplay is not null;
        var virtualConnected = _virtualDevice.IsConnected;
        var playback = _virtualSequence?.IsRunning == true;
        var fanMonitor = _fanMonitorTask is { IsCompleted: false };
        var summary = RuntimeDiagnosticsSnapshot.Format(
            OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsLinux() ? "Linux" : "Other",
            virtualConnected, serial, playback, fanMonitor, _virtualDevice.CommandCount);
        this.FindControl<global::Avalonia.Controls.TextBlock>("DiagnosticsSnapshot")!.Text = summary;
    }
}
