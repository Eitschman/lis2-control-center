namespace LIS2.App.Avalonia;

/// <summary>WPF-compatible single-page presets; selection never touches a physical device.</summary>
public static class HardwareDisplayPresetBuilder
{
    public static HardwareDisplayPresetPage? Build(string preset,
        IReadOnlyDictionary<string, object?> snapshot,
        IEnumerable<PortableHardwareSensorPreference> preferences)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(preferences);
        // The catalog has already rejected non-finite/non-numeric readings.
        // Keep its normalized unit and availability semantics for all presets.
        var entries = HardwareSensorCatalog.Build(snapshot, preferences)
            .Where(entry => entry.IsAvailable)
            .ToArray();

        HardwareSensorEntry? First(string type, params string[] terms) =>
            entries.Where(entry => string.Equals(entry.SensorType, type, StringComparison.OrdinalIgnoreCase) &&
                (terms.Length == 0 || terms.Any(term =>
                    entry.Key.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    entry.Alias.Contains(term, StringComparison.OrdinalIgnoreCase))))
                .OrderByDescending(entry => entry.IsFavorite).FirstOrDefault();

        HardwareSensorEntry[] FirstTwo(string type) =>
            entries.Where(entry => string.Equals(entry.SensorType, type, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(entry => entry.IsFavorite).Take(2).ToArray();

        static string Line(string label, HardwareSensorEntry entry) =>
            label + " {" + entry.Key + "}" + entry.Unit;

        HardwareDisplayPresetPage? Pair(string name, HardwareSensorEntry? first,
            HardwareSensorEntry? second, string label1, string label2)
        {
            if (first is null && second is null) return null;
            return new HardwareDisplayPresetPage(name, first is null ? name : Line(label1, first),
                second is null ? "" : Line(label2, second));
        }

        HardwareDisplayPresetPage? Two(string name, IReadOnlyList<HardwareSensorEntry> items,
            string label1, string label2)
        {
            if (items.Count == 0) return null;
            return new HardwareDisplayPresetPage(name, Line(label1, items[0]),
                items.Count > 1 ? Line(label2, items[1]) : "");
        }

        return preset switch
        {
            "CPU" => Pair("CPU", First("Load", "CPU", "Processor"),
                First("Temperature", "CPU", "Processor"), "CPU", "Temp"),
            "GPU" => Pair("GPU", First("Load", "GPU", "Graphics"),
                First("Temperature", "GPU", "Graphics"), "GPU", "Temp"),
            "Memory" => Pair("Memory", First("Load", "Memory", "RAM"),
                First("Data", "Memory", "RAM"), "RAM", "Used"),
            "Temperatures" => Two("Temperatures", FirstTwo("Temperature"), "T1", "T2"),
            "Fans" => Two("Fans", FirstTwo("Fan"), "Fan1", "Fan2"),
            _ => throw new ArgumentOutOfRangeException(nameof(preset))
        };
    }
}

public sealed record HardwareDisplayPresetPage(string Name, string Line1Template, string Line2Template);
