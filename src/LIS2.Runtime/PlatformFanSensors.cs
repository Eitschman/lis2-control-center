using System.Globalization;

namespace LIS2.Runtime;

/// <summary>
/// Linux hwmon temperature sensor snapshots. Stable keys use the hwmon device name,
/// temperature input index and optional label; never the unstable hwmon directory number.
/// Unavailable or unreadable sensors are omitted, triggering FanRuntime fail-safe.
/// </summary>
public sealed class LinuxHwmonFanSensors : IFanSensorSource
{
    private readonly string _root;
    public LinuxHwmonFanSensors(string root = "/sys/class/hwmon") => _root = root;

    public IReadOnlyDictionary<string, object?> Snapshot()
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(_root)) return values;
        foreach (var device in Directory.EnumerateDirectories(_root))
        {
            try
            {
                var name = File.ReadAllText(Path.Combine(device, "name")).Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;
                foreach (var input in Directory.EnumerateFiles(device, "temp*_input"))
                {
                    try
                    {
                        var suffix = Path.GetFileName(input)["temp".Length..^"_input".Length];
                        if (!int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index < 1) continue;
                        var text = File.ReadAllText(input).Trim();
                        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var milli) ||
                            !double.IsFinite(milli)) continue;
                        var key = $"Hardware.{Normalize(name)}.Temperature.temp{index}";
                        values.TryAdd(key, milli / 1000d);
                        var labelPath = Path.Combine(device, $"temp{index}_label");
                        if (File.Exists(labelPath))
                        {
                            var label = Normalize(File.ReadAllText(labelPath).Trim());
                            if (label.Length > 0) values.TryAdd($"Hardware.{Normalize(name)}.Temperature.{label}", milli / 1000d);
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { }
        }
        return values;
    }

    private static string Normalize(string name) =>
        new(name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
}

/// <summary>Explicit read-only sensor adapter for existing source snapshots.</summary>
public sealed class SnapshotFanSensors : IFanSensorSource
{
    private readonly Func<IReadOnlyDictionary<string, object?>> _read;
    public SnapshotFanSensors(Func<IReadOnlyDictionary<string, object?>> read) =>
        _read = read ?? throw new ArgumentNullException(nameof(read));
    public IReadOnlyDictionary<string, object?> Snapshot() => _read();
}
