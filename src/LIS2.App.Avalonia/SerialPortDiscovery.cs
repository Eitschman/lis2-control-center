using System.IO.Ports;

namespace LIS2.App.Avalonia;

/// <summary>Finds stable serial device paths without opening or writing to hardware.</summary>
public static class SerialPortDiscovery
{
    public static IReadOnlyList<string> GetAvailablePorts()
    {
        var ports = SerialPort.GetPortNames();
        if (!OperatingSystem.IsLinux())
            return ports.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();

        return PreferStableLinuxPaths(ports, Directory.Exists("/dev/serial/by-id")
            ? Directory.EnumerateFiles("/dev/serial/by-id").ToArray()
            : Array.Empty<string>());
    }

    public static IReadOnlyList<string> PreferStableLinuxPaths(
        IEnumerable<string> portNames, IEnumerable<string> byIdPaths)
    {
        ArgumentNullException.ThrowIfNull(portNames);
        ArgumentNullException.ThrowIfNull(byIdPaths);
        var result = new List<string>();
        var covered = new HashSet<string>(StringComparer.Ordinal);
        foreach (var link in byIdPaths.OrderBy(p => p, StringComparer.Ordinal))
        {
            try
            {
                var target = File.ResolveLinkTarget(link, true);
                if (target is null) continue;
                var resolved = Path.GetFullPath(target.FullName);
                if (covered.Add(resolved)) result.Add(link);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        foreach (var port in portNames.OrderBy(p => p, StringComparer.Ordinal))
        {
            if (!covered.Contains(Path.GetFullPath(port)) && !result.Contains(port, StringComparer.Ordinal))
                result.Add(port);
        }
        return result;
    }
}
