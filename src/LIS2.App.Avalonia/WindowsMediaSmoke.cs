namespace LIS2.App.Avalonia;

/// <summary>Prüft den tatsächlich mitgelieferten GSMTC-Anbieter ohne Workspace oder Gerätezugriff.</summary>
internal static class WindowsMediaSmoke
{
    internal static async Task<int> RunAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        try
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("GSMTC smoke requires Windows.");
            await using var bridge = new WindowsMediaBridge();
            await bridge.StartAsync().WaitAsync(TimeSpan.FromSeconds(15));
            if (!bridge.IsAvailable || !bridge.Values.TryGetValue("Connected", out var connected) || connected is not bool)
                throw new InvalidOperationException("GSMTC provider did not supply its connection state.");
            await File.WriteAllTextAsync(Path.Combine(directory, "provider.txt"), "PASS: packaged provider loaded; Connected=" + connected);
            await bridge.DisposeAsync();
            if (bridge.IsAvailable || !Equals(bridge.Values["Connected"], false))
                throw new InvalidOperationException("GSMTC provider remained active after disposal.");
            try
            {
                await bridge.StartAsync(Path.Combine(directory, "missing-provider.dll"));
                throw new InvalidOperationException("Missing GSMTC provider was not reported.");
            }
            catch (FileNotFoundException) { /* Die fehlende DLL muss ausdrücklich gemeldet werden. */ }
            await File.WriteAllTextAsync(Path.Combine(directory, "result.txt"), "PASS: packaged GSMTC startup, connection state, shutdown and missing DLL.");
            return 0;
        }
        catch (Exception exception)
        {
            // Bei defekten Reflection-Abhängigkeiten kann selbst das Formatieren eines Stacktraces fehlschlagen.
            IEnumerable<Exception> failures = exception is AggregateException aggregate ? aggregate.Flatten().InnerExceptions : [exception];
            await File.WriteAllLinesAsync(Path.Combine(directory, "failure.txt"), failures.Select(error => error.GetType().FullName + ": " + error.Message));
            return 1;
        }
    }
}
