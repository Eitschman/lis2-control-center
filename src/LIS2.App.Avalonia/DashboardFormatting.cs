using System.Text.RegularExpressions;

namespace LIS2.App.Avalonia;

internal static class DashboardFormatting
{
    /// <summary>Passt nur ausdrücklich ausgewählte Zahlenformate im Dialogentwurf an; freie Texte bleiben erhalten.</summary>
    internal static string CompactTemplate(string template) => Regex.Replace(template, @"\{[^{}]+\}", match =>
    {
        var parts = match.Value[1..^1].Split('|').ToList();
        var numeric = false;
        for (var index = 1; index < parts.Count; index++)
        {
            var operation = parts[index].Split(':', 2);
            if (operation.Length != 2 || !int.TryParse(operation[1], out var decimals) || decimals < 0 || decimals > 6) continue;
            var name = operation[0].Trim().ToLowerInvariant();
            if (name is not ("number" or "percent" or "bytes")) continue;
            numeric = true;
            parts[index] = (name == "percent" ? "number" : name) + ":" + Math.Min(decimals, 1);
            if (name == "percent") parts.Insert(++index, "suffix: %");
        }
        if (numeric)
            for (var index = 1; index < parts.Count; index++)
                if (parts[index] is "suffix:°C" or "suffix:°F" or "suffix:%" or "suffix:RPM" or "suffix:Hz")
                    parts[index] = parts[index].Insert(7, " ");
        return "{" + string.Join('|', parts) + "}";
    });
}
