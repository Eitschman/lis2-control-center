using Microsoft.Win32;

namespace LIS2.App;

public sealed class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LIS2ControlCenter";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string value &&
               !string.IsNullOrWhiteSpace(value);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException(
                "Unable to open the current-user Windows startup registry key.");

        if (!enabled)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var executable = Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(executable))
            throw new InvalidOperationException("Unable to determine the application executable path.");

        var fileName = System.IO.Path.GetFileName(executable);

        if (string.Equals(fileName, "dotnet.exe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileName, "testhost.exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Windows autostart is available only when running the published LIS2ControlCenter.exe.");
        }

        key.SetValue(
            ValueName,
            $"\"{executable}\" --minimized",
            RegistryValueKind.String);
    }
}
