namespace LIS2.App.Avalonia;

/// <summary>Lesende Prüfschritte verwenden bestätigte Quellzustände; sie öffnen keinen seriellen Port.</summary>
internal static class ConnectionTestPlans
{
    internal static IReadOnlyList<ConnectionTestStep> Serial(string? port, bool connected, Func<IReadOnlyList<string>> discovery) =>
    [
        ConnectionTest.Check("Port selection", !string.IsNullOrWhiteSpace(port), "No serial port selected", "Select and apply a serial port in Settings"),
        new("Port discovery", "Check the USB connection, serial driver and port selection", cancellation => Task.Run<string?>(() =>
        {
            cancellation.ThrowIfCancellationRequested();
            return discovery().Contains(port, OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                ? null : "The configured serial port was not found";
        }, cancellation)),
        ConnectionTest.Check("Open serial connection", connected, "The configured serial connection is not open",
            "Use Apply / reconnect in Settings; check port permissions and other applications")
    ];

    internal static IReadOnlyList<ConnectionTestStep> Hardware(IReadOnlyDictionary<string, object?> values, bool fresh) =>
    [
        ConnectionTest.Check("Sensor acquisition", values.Any(pair => HardwareSensorCatalog.IsSensorKey(pair.Key) &&
            double.TryParse(Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number) && double.IsFinite(number)),
            "No valid hardware measurements", "Open Hardware and check sensor availability and operating system permissions"),
        ConnectionTest.Check("Measurement freshness", fresh, "Hardware measurements are stale", "Refresh Hardware and check the sensor provider")
    ];

    internal static IReadOnlyList<ConnectionTestStep> Media(string source, IReadOnlyDictionary<string, object?> values,
        IReadOnlyDictionary<string, object?> audio, bool providerAvailable, Func<string, bool> executableAvailable)
    {
        bool Flag(IReadOnlyDictionary<string, object?> data, string key) => data.GetValueOrDefault(key) is true;
        var linux = source == "Media";
        var legacy = source == "WmpLegacy";
        var modern = source == "WindowsMedia";
        var playing = string.Equals(values.GetValueOrDefault("State")?.ToString(), "Playing", StringComparison.OrdinalIgnoreCase);
        var steps = new List<ConnectionTestStep>();
        if (linux)
            steps.Add(ConnectionTest.Check("playerctl", executableAvailable("playerctl"), "playerctl is missing", "Install playerctl and start the selected player in the desktop session"));
        else if (modern)
            steps.Add(ConnectionTest.Check("GSMTC provider", providerAvailable, "The Windows media provider is unavailable", "Use the Windows release package and check the Diagnostics report"));
        steps.Add(ConnectionTest.Check(linux || modern ? "Player session" : "Metadata pipe", Flag(values, "Connected"),
            "No connection to the selected player", linux ? "Start the selected MPRIS player in the same desktop session" : modern ?
            "Start Windows Media Player and select a media file" : legacy ?
            "Register and enable the background plug-in matching the Legacy player architecture" : "Replace gen_lis2.dll while Winamp is closed, then restart Winamp"));
        if (!modern)
        {
            var validAge = values.GetValueOrDefault("SnapshotAgeSeconds") is { } age && double.TryParse(Convert.ToString(age,
                System.Globalization.CultureInfo.InvariantCulture), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var seconds) && double.IsFinite(seconds) && seconds >= 0 && seconds < 5;
            steps.Add(ConnectionTest.Check("Metadata freshness", validAge, "Player metadata is stale", "Check the player and its metadata connection"));
            if (linux)
                foreach (var executable in new[] { "pw-dump", "pw-record", "dbus-send" })
                    steps.Add(ConnectionTest.Check(executable, executableAvailable(executable),
                        "Required Linux audio tools are missing", "Install pipewire-bin and dbus-bin and use a PipeWire desktop session") with { IsApplicable = playing });
            var audioConnected = linux ? Flag(audio, "AudioConnected") : legacy ? Flag(values, "VisualizationConnected") :
                values.GetValueOrDefault("VuLeft") is not null && values.GetValueOrDefault("VuRight") is not null;
            steps.Add(ConnectionTest.Check("Player audio", audioConnected, "No current audio from the selected player",
                linux ? "Check PipeWire and the audio status on the Media page" : legacy ?
                "Use Windows build 20348 or later and check the Legacy audio status on the Media page" :
                "Start playback and check the installed Winamp plug-in") with { IsApplicable = playing });
        }
        return steps;
    }

    internal static bool ExecutableAvailable(string name) => (Environment.GetEnvironmentVariable("PATH") ?? "")
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Any(directory => File.Exists(Path.Combine(directory, name)));
}
