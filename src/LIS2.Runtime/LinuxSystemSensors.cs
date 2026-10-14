using System.Diagnostics;
using System.Globalization;

namespace LIS2.Runtime;

/// <summary>Read-only Linux system metrics for display templates. Rate counters require two snapshots.</summary>
public sealed class LinuxSystemSensors : IFanSensorSource
{
    private readonly LinuxHwmonFanSensors _hwmon;
    private readonly string _proc;
    private readonly string _sys;
    private readonly Func<double> _seconds;
    private readonly Func<IEnumerable<DriveInfo>> _drives;
    private readonly Dictionary<string, (ulong idle, ulong total)> _cpuPrevious = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (ulong read, ulong write)> _diskPrevious = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (ulong rx, ulong tx)> _networkPrevious = new(StringComparer.Ordinal);
    private double? _lastSeconds;

    public LinuxSystemSensors(string proc = "/proc", string sys = "/sys", Func<double>? seconds = null,
        Func<IEnumerable<DriveInfo>>? drives = null)
    {
        _proc = proc;
        _sys = sys;
        _hwmon = new LinuxHwmonFanSensors(Path.Combine(sys, "class/hwmon"));
        _seconds = seconds ?? (() => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency);
        _drives = drives ?? (() => DriveInfo.GetDrives());
    }

    public IReadOnlyDictionary<string, object?> Snapshot()
    {
        var result = new Dictionary<string, object?>(_hwmon.Snapshot(), StringComparer.OrdinalIgnoreCase);
        var now = _seconds();
        var elapsed = _lastSeconds is double previous ? now - previous : 0;
        _lastSeconds = now;
        ReadCpu(result);
        ReadMemory(result);
        ReadCpuFrequencies(result);
        ReadDisks(result, elapsed);
        ReadNetworks(result, elapsed);
        ReadFilesystems(result);
        ReadHwmonFans(result);
        return result;
    }

    private static void Add(IDictionary<string, object?> result, string key, double value, string unit)
    {
        if (!double.IsFinite(value)) return;
        result[key] = value;
        result[key + ".Unit"] = unit;
    }

    private static string Safe(string text) => new(text.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

    private void ReadCpu(IDictionary<string, object?> values)
    {
        try
        {
            foreach (var line in File.ReadLines(Path.Combine(_proc, "stat")))
            {
                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5 || !parts[0].StartsWith("cpu", StringComparison.Ordinal)) continue;
                if (!parts[0].Skip(3).All(char.IsDigit)) continue;
                var counters = parts.Skip(1).Select(s => ulong.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                var idle = counters[3] + (counters.Length > 4 ? counters[4] : 0UL);
                var total = counters.Aggregate(0UL, (a, b) => a + b);
                if (_cpuPrevious.TryGetValue(parts[0], out var old) && total > old.total && idle >= old.idle)
                    Add(values, "Hardware.CPU.Load." + (parts[0] == "cpu" ? "Total" : parts[0]), 100d * (1d - (double)(idle - old.idle) / (total - old.total)), "%");
                _cpuPrevious[parts[0]] = (idle, total);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException or OverflowException) { }
    }

    private void ReadMemory(IDictionary<string, object?> values)
    {
        try
        {
            var mem = new Dictionary<string, ulong>(StringComparer.Ordinal);
            foreach (var line in File.ReadLines(Path.Combine(_proc, "meminfo")))
            {
                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1 && ulong.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var kb))
                    mem[parts[0].TrimEnd(':')] = kb * 1024UL;
            }
            if (mem.TryGetValue("MemTotal", out var total) && mem.TryGetValue("MemAvailable", out var available) && total > 0)
            {
                Add(values, "Hardware.Memory.Data.Total", total, "B");
                Add(values, "Hardware.Memory.Data.Used", total - Math.Min(total, available), "B");
                Add(values, "Hardware.Memory.Data.Available", available, "B");
                Add(values, "Hardware.Memory.Load.Used", 100d * (total - Math.Min(total, available)) / total, "%");
            }
            if (mem.TryGetValue("SwapTotal", out var swap) && mem.TryGetValue("SwapFree", out var free))
            {
                Add(values, "Hardware.Swap.Data.Total", swap, "B");
                Add(values, "Hardware.Swap.Data.Used", swap - Math.Min(swap, free), "B");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OverflowException) { }
    }

    private void ReadCpuFrequencies(IDictionary<string, object?> values)
    {
        try
        {
            foreach (var cpu in Directory.EnumerateDirectories(Path.Combine(_sys, "devices/system/cpu"), "cpu*"))
            {
                var name = Path.GetFileName(cpu);
                if (name.Length <= 3 || !name.StartsWith("cpu", StringComparison.Ordinal) || !name.Skip(3).All(char.IsDigit)) continue;
                var file = Path.Combine(cpu, "cpufreq/scaling_cur_freq");
                if (File.Exists(file) && double.TryParse(File.ReadAllText(file).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var khz))
                    Add(values, "Hardware.CPU.Clock." + name, khz / 1000d, "MHz");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    private void ReadDisks(IDictionary<string, object?> values, double elapsed)
    {
        try
        {
            foreach (var line in File.ReadLines(Path.Combine(_proc, "diskstats")))
            {
                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 14) continue;
                var name = parts[2];
                if (!ulong.TryParse(parts[5], out var sectorsRead) || !ulong.TryParse(parts[9], out var sectorsWritten)) continue;
                var key = Safe(name);
                if (_diskPrevious.TryGetValue(name, out var old) && elapsed > 0 && sectorsRead >= old.read && sectorsWritten >= old.write)
                {
                    Add(values, "Hardware.Disk_" + key + ".Throughput.Read", (sectorsRead - old.read) * 512d / elapsed, "B/s");
                    Add(values, "Hardware.Disk_" + key + ".Throughput.Write", (sectorsWritten - old.write) * 512d / elapsed, "B/s");
                }
                _diskPrevious[name] = (sectorsRead, sectorsWritten);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    private void ReadNetworks(IDictionary<string, object?> values, double elapsed)
    {
        try
        {
            foreach (var line in File.ReadLines(Path.Combine(_proc, "net/dev")).Skip(2))
            {
                var colon = line.IndexOf(':');
                if (colon < 0) continue;
                var name = line[..colon].Trim();
                if (name == "lo") continue;
                var parts = line[(colon + 1)..].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 16 || !ulong.TryParse(parts[0], out var rx) || !ulong.TryParse(parts[8], out var tx)) continue;
                if (_networkPrevious.TryGetValue(name, out var old) && elapsed > 0 && rx >= old.rx && tx >= old.tx)
                {
                    Add(values, "Hardware.Network_" + Safe(name) + ".Throughput.Receive", (rx - old.rx) / elapsed, "B/s");
                    Add(values, "Hardware.Network_" + Safe(name) + ".Throughput.Send", (tx - old.tx) / elapsed, "B/s");
                }
                _networkPrevious[name] = (rx, tx);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    private void ReadFilesystems(IDictionary<string, object?> values)
    {
        try
        {
            foreach (var drive in _drives())
            {
                try
                {
                    if (!drive.IsReady || drive.DriveType != DriveType.Fixed || drive.TotalSize <= 0) continue;
                    var key = drive.Name == "/" ? "Root" : Safe(drive.Name.Trim('/'));
                    if (key.Length == 0) continue;
                    var prefix = "Hardware.Filesystem_" + key;
                    Add(values, prefix + ".Data.Total", drive.TotalSize, "B");
                    Add(values, prefix + ".Data.Free", drive.AvailableFreeSpace, "B");
                    Add(values, prefix + ".Data.Used", drive.TotalSize - drive.AvailableFreeSpace, "B");
                    Add(values, prefix + ".Load.Used", 100d * (drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize, "%");
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    private void ReadHwmonFans(IDictionary<string, object?> values)
    {
        try
        {
            foreach (var device in Directory.EnumerateDirectories(Path.Combine(_sys, "class/hwmon")))
            {
                try
                {
                    var name = Safe(File.ReadAllText(Path.Combine(device, "name")).Trim());
                    if (name.Length == 0) continue;
                    foreach (var input in Directory.EnumerateFiles(device, "fan*_input"))
                    {
                        try
                        {
                            var number = Path.GetFileName(input)["fan".Length..^"_input".Length];
                            if (!int.TryParse(number, out var index) || index < 1) continue;
                            if (double.TryParse(File.ReadAllText(input).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var rpm))
                                Add(values, "Hardware." + name + ".Fan.fan" + index, rpm, "RPM");
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
