namespace LIS2.App.Avalonia;

/// <summary>Pure, read-only projection of already-known application state.</summary>
public static class RuntimeDiagnosticsSnapshot
{
    public static string Format(string platform, bool virtualConnected, bool serialConnected,
        bool virtualPlaybackRunning, bool fanMonitorRunning, int virtualCommands,
        bool serialBusy = false, bool sensorRefreshRunning = false, int configuredFanSensors = 0, Func<string, string>? translate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);
        string Text(string source) => translate?.Invoke(source) ?? source;
        return string.Join(Environment.NewLine, new[]
        {
            Text("Platform: ") + platform,
            Text("Virtual transport: ") + (virtualConnected ? Text("Connected") : Text("Disconnected")),
            Text("Serial transport: ") + (serialConnected ? Text("Connected") : Text("Disconnected")),
            Text("Virtual page playback: ") + (virtualPlaybackRunning ? Text("Running") : Text("Stopped")),
            Text("Fan dry-run monitor: ") + (fanMonitorRunning ? Text("Running") : Text("Stopped")),
            Text("Virtual commands: ") + Math.Max(0, virtualCommands),
            Text("Serial operation: ") + (serialBusy ? Text("Busy") : Text("Idle")),
            Text("Hardware sensor live refresh: ") + (sensorRefreshRunning ? Text("Running") : Text("Stopped")),
            Text("Configured fan sensor channels: ") + Math.Clamp(configuredFanSensors, 0, 4),
            Text("Physical fan authorization: Not displayed; inspect Fan Control before any action.")
        });
    }
}
