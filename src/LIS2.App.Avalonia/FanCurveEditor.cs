using System.Globalization;

namespace LIS2.App.Avalonia;

/// <summary>Text- und Punkteeditor verwenden dieselben präzisen Temperaturen und ganzzahligen Ausgaben.</summary>
public static class FanCurveEditor
{
    public static List<PortableFanCurvePoint> Parse(string? text)
    {
        var points = new List<PortableFanCurvePoint>();
        foreach (var line in (text ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split('=');
            if (parts.Length != 2 || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var temperature) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var output) ||
                !double.IsFinite(temperature) || output is < 0 or > 100)
                throw new FormatException("Curve point must be temperature=percent.");
            points.Add(new PortableFanCurvePoint(temperature, output));
        }
        // Derselbe letzte Wert je Temperatur wie bei der Normalisierung gespeicherter Kanäle.
        return points.GroupBy(point => point.Temperature).Select(group => group.Last()).OrderBy(point => point.Temperature).ToList();
    }

    public static string Format(IEnumerable<PortableFanCurvePoint> points) => string.Join(Environment.NewLine,
        points.Select(point => point.Temperature.ToString("R", CultureInfo.InvariantCulture) + "=" + point.OutputPercent.ToString(CultureInfo.InvariantCulture)));
}
