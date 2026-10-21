using System.Text;

namespace LIS2.App.Avalonia;

/// <summary>Verwaltet ausschließlich den eigenen Autostarteintrag im Benutzerprofil.</summary>
internal sealed class DesktopAutostart
{
    internal string EntryPath { get; }

    internal DesktopAutostart(string configDirectory)
    {
        EntryPath = Path.Combine(configDirectory, "autostart", "lis2-control-center.desktop");
    }

    internal static DesktopAutostart ForCurrentUser()
    {
        var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrWhiteSpace(config) || !Path.IsPathRooted(config))
            config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        return new DesktopAutostart(config);
    }

    internal bool IsEnabled => File.Exists(EntryPath) && !File.ReadLines(EntryPath)
        .Any(line => line.Trim().Equals("Hidden=true", StringComparison.OrdinalIgnoreCase));

    internal void SetEnabled(bool enabled, string executable, string? managedAssembly = null)
    {
        if (!enabled)
        {
            if (File.Exists(EntryPath)) File.Delete(EntryPath);
            return;
        }
        if (!Path.IsPathFullyQualified(executable) || executable.Contains('=') || executable.Any(character => char.IsControl(character)))
            throw new ArgumentException("Executable path must be absolute and contain neither control characters nor an equals sign.", nameof(executable));
        if (managedAssembly is not null && (!Path.IsPathFullyQualified(managedAssembly) || managedAssembly.Any(char.IsControl)))
            throw new ArgumentException("Assembly path must be absolute and contain no control characters.", nameof(managedAssembly));
        // Desktop Entry verwendet zuerst String-Escapes und danach Exec-Argument-Escapes, keine Shell.
        static string Quote(string path) => ("\"" + path.Replace("\\", "\\\\").Replace("\"", "\\\"")
            .Replace("`", "\\`").Replace("$", "\\$").Replace("%", "%%") + "\"").Replace("\\", "\\\\");
        var arguments = Quote(executable) + (managedAssembly is null ? "" : " " + Quote(managedAssembly));
        Directory.CreateDirectory(Path.GetDirectoryName(EntryPath)!);
        File.WriteAllText(EntryPath, "[Desktop Entry]\nType=Application\nName=LIS2 Control Center\nExec=" +
            arguments + " --minimized\nTerminal=false\n", new UTF8Encoding(false));
    }
}
