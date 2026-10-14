namespace LIS2.App.Avalonia;

public static class HardwareDisplayPresetBuilder
{
    public static IReadOnlyList<string> SelectKeys(IReadOnlyDictionary<string, object?> sensors, string preset)
    {
        ArgumentNullException.ThrowIfNull(sensors);
        var group = preset switch
        {
            "CPU" => "cpu",
            "GPU" => "gpu",
            "Memory" => "memory",
            "Temperatures" => "temperature",
            "Fans" => "fan",
            _ => throw new ArgumentOutOfRangeException(nameof(preset))
        };
        return sensors.Keys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Where(key => key.Split('.', '/', ':').Any(segment =>
                string.Equals(segment, group, StringComparison.OrdinalIgnoreCase) ||
                (preset == "Memory" && string.Equals(segment, "ram", StringComparison.OrdinalIgnoreCase))))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<HardwareDisplayPresetPage> Build(string preset, IReadOnlyList<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        return keys.Select((key, index) => new HardwareDisplayPresetPage(
            preset + " " + (index + 1), key, "{" + key + "}")).ToArray();
    }
}

public sealed record HardwareDisplayPresetPage(string Name, string Line1Template, string Line2Template);
