using System.Reflection;

namespace LIS2.App.Avalonia;

/// <summary>Fehler bleiben nach Prozessende untersuchbar, ohne Ausnahme-Nachrichten mit möglichen Tokens zu speichern.</summary>
public static class FailureLog
{
    private static readonly object Sync = new();
    public static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LIS2ControlCenter", "avalonia-errors.log");

    public static bool Write(string context, Exception exception, string? path = null)
    {
        try
        {
            lock (Sync)
            {
                var destination = path ?? FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                // Message, Data und ToString werden bewusst nicht serialisiert: sie können Zugangsdaten enthalten.
                File.AppendAllText(destination, $"{DateTimeOffset.UtcNow:O} [{context}] Version={version}{Environment.NewLine}" +
                    exception.GetType().FullName + Environment.NewLine + exception.StackTrace + Environment.NewLine);
            }
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            System.Diagnostics.Trace.TraceError("Error log write failed: {0}", error.GetType().Name);
            return false;
        }
    }
}
