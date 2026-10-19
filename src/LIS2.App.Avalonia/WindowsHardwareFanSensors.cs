using LibreHardwareMonitor.Hardware;
using LIS2.Runtime;

namespace LIS2.App.Avalonia;

/// <summary>
/// Windows-only temperature sensor snapshot, reusing the WPF application's
/// LibreHardwareMonitor key format. Each snapshot refreshes the hardware,
/// so stale cached temperatures are never used for fan regulation.
/// </summary>
public sealed class WindowsHardwareFanSensors : IFanSensorSource, IDisposable
{
    private readonly Computer _computer;
    private bool _disposed;

    public WindowsHardwareFanSensors()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Windows hardware sensors require Windows.");
        _computer = new Computer
        {
            IsCpuEnabled = true, IsGpuEnabled = true, IsMemoryEnabled = true,
            IsMotherboardEnabled = true, IsStorageEnabled = true,
            IsNetworkEnabled = true, IsControllerEnabled = true
        };
        _computer.Open();
    }

    public IReadOnlyDictionary<string, object?> Snapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var hardware in _computer.Hardware)
            Read(hardware, values);
        return values;
    }

    private static void Read(IHardware hardware, Dictionary<string, object?> values)
    {
        hardware.Update();
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.Value is not float value || !float.IsFinite(value))
                continue;
            var key = $"Hardware.{Normalize(hardware.Name)}.{sensor.SensorType}.{Normalize(sensor.Name)}";
            values[key] = value;
            values[key + ".Unit"] = Unit(sensor.SensorType);
        }
        foreach (var child in hardware.SubHardware)
            Read(child, values);
    }

    private static string Unit(SensorType type) => type switch
    {
        SensorType.Temperature => "C",
        SensorType.Load => "%",
        SensorType.Fan => "RPM",
        SensorType.Clock => "MHz",
        SensorType.Voltage => "V",
        SensorType.Power => "W",
        SensorType.Data => "GB",
        SensorType.SmallData => "MB",
        SensorType.Throughput => "B/s",
        _ => string.Empty
    };

    private static string Normalize(string value)
    {
        var chars = value.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        return new string(chars).Trim('_').Replace("__", "_", StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _computer.Close();
    }
}
