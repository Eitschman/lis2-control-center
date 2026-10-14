namespace LIS2.App.Avalonia;

/// <summary>Pure, read-only projection of already-known application state.</summary>
public static class RuntimeDiagnosticsSnapshot
{
    public static string Format(string platform, bool virtualConnected, bool serialConnected,
        bool virtualPlaybackRunning, bool fanMonitorRunning, int virtualCommands,
        bool serialBusy = false, bool sensorRefreshRunning = false, int configuredFanSensors = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);
        return string.Join(Environment.NewLine, new[]
        {
            "Platform: " + platform,
            "Virtual transport: " + (virtualConnected ? "Connected" : "Disconnected"),
            "Serial transport: " + (serialConnected ? "Connected" : "Disconnected"),
            "Virtual page playback: " + (virtualPlaybackRunning ? "Running" : "Stopped"),
            "Fan dry-run monitor: " + (fanMonitorRunning ? "Running" : "Stopped"),
            "Virtual commands: " + Math.Max(0, virtualCommands),
            "Serial operation: " + (serialBusy ? "Busy" : "Idle"),
            "Hardware sensor live refresh: " + (sensorRefreshRunning ? "Running" : "Stopped"),
            "Configured fan sensor channels: " + Math.Clamp(configuredFanSensors, 0, 4),
            "Physical fan authorization: Not displayed; inspect Fan Control before any action."
        });
    }
}
