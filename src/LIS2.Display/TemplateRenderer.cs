using System.Globalization;
using System.Text.RegularExpressions;

namespace LIS2.Display;

public sealed partial class TemplateRenderer
{
    [GeneratedRegex(@"{(?<expression>[^{}]+)}", RegexOptions.CultureInvariant)]
    private static partial Regex VariablePattern();

    public string Render(string template, IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);

        return VariablePattern().Replace(template, match =>
        {
            var expression = match.Groups["expression"].Value;
            return RenderExpression(expression, values);
        });
    }

    public DisplayFrame RenderFrame(
        string line1Template,
        string line2Template,
        IReadOnlyDictionary<string, object?> values) =>
        DisplayFrame.Create(
            Render(line1Template, values),
            Render(line2Template, values));

    private static string RenderExpression(
        string expression,
        IReadOnlyDictionary<string, object?> values)
    {
        // Textargumente enthalten gewollte Abstände, etwa "Core 10: ".
        // Nur Schlüssel und Operationsnamen bereinigen, nicht den Text nach dem Doppelpunkt.
        var parts = expression.Split('|');

        if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            return string.Empty;

        var name = parts[0].Trim();
        values.TryGetValue(name, out var rawValue);

        // Ersatzvariablen sind reine Schlüssel, keine rekursiv auszuwertenden Vorlagen.
        foreach (var operation in parts.Skip(1).Select(ParseOperation))
        {
            if (!IsMissingValue(rawValue)) break;
            if (string.Equals(operation.Name, "fallbackvar", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(operation.Argument))
            {
                name = operation.Argument.Trim();
                values.TryGetValue(name, out rawValue);
            }
        }

        var fallback = parts
            .Skip(1)
            .Select(ParseOperation)
            .FirstOrDefault(operation =>
                string.Equals(
                    operation.Name,
                    "fallback",
                    StringComparison.OrdinalIgnoreCase))
            .Argument;

        if (IsMissingValue(rawValue))
            return fallback ?? string.Empty;

        object? current = rawValue;

        // HA-Zustände sind Protokolltexte mit Dezimalpunkt. Nur ihre Darstellung
        // lokalisieren; Rohdaten, Vergleiche, Textattribute und Präzision bleiben erhalten.
        string DisplayText(object? value) =>
            SourceValueFormatter.Format(value, ReferenceEquals(value, rawValue) ? name : null, values);

        foreach (var part in parts.Skip(1))
        {
            var operation = ParseOperation(part);

            switch (operation.Name.ToLowerInvariant())
            {
                case "fallback":
                case "fallbackvar":
                    break;

                case "number":
                    current = FormatNumber(current, operation.Argument);
                    break;

                case "percent":
                    current = FormatPercent(current, operation.Argument);
                    break;

                case "bytes":
                    current = FormatBytes(current, operation.Argument);
                    break;

                case "upper":
                    current = DisplayText(current)
                        .ToUpper(CultureInfo.CurrentCulture);
                    break;

                case "lower":
                    current = DisplayText(current)
                        .ToLower(CultureInfo.CurrentCulture);
                    break;

                case "prefix":
                    current = (operation.Argument ?? string.Empty) +
                              DisplayText(current);
                    break;

                case "suffix":
                    current = DisplayText(current) +
                              (operation.Argument ?? string.Empty);
                    break;
            }
        }

        var result = DisplayText(current);
        return string.IsNullOrEmpty(result) ? fallback ?? string.Empty : result;
    }

    /// <summary>Leere Werte und explizite Quellenzustände gelten als fehlend; gewollte Leerzeichen bleiben Werte.</summary>
    public static bool IsMissingValue(object? value) => value is null || value is string text &&
        (text.Length == 0 || text.Equals("unknown", StringComparison.OrdinalIgnoreCase) ||
         text.Equals("unavailable", StringComparison.OrdinalIgnoreCase));

    private static (string Name, string? Argument) ParseOperation(string value)
    {
        var separator = value.IndexOf(':');
        return separator < 0
            ? (value.Trim(), null)
            : (value[..separator].Trim(), value[(separator + 1)..]);
    }

    private static string FormatNumber(object? value, string? decimals)
    {
        if (!TryConvertNumber(value, out var number))
            return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;

        var digits = ParseDigits(decimals, 0);
        return number.ToString($"F{digits}", CultureInfo.CurrentCulture);
    }

    private static string FormatPercent(object? value, string? decimals)
    {
        if (!TryConvertNumber(value, out var number))
            return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;

        var digits = ParseDigits(decimals, 0);
        return number.ToString($"F{digits}", CultureInfo.CurrentCulture) + "%";
    }

    private static string FormatBytes(object? value, string? decimals)
    {
        if (!TryConvertNumber(value, out var bytes))
            return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;

        var digits = ParseDigits(decimals, 1);
        var absolute = Math.Abs(bytes);
        var units = new[] { "B", "KB", "MB", "GB", "TB", "PB" };
        var unit = 0;

        while (absolute >= 1024 && unit < units.Length - 1)
        {
            bytes /= 1024;
            absolute /= 1024;
            unit++;
        }

        return bytes.ToString($"F{digits}", CultureInfo.CurrentCulture) +
               " " +
               units[unit];
    }

    private static int ParseDigits(string? value, int fallback) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? Math.Clamp(result, 0, 6)
            : fallback;

    private static bool TryConvertNumber(object? value, out double number)
    {
        try
        {
            number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return !double.IsNaN(number) && !double.IsInfinity(number);
        }
        catch (Exception ex) when (
            ex is FormatException or InvalidCastException or OverflowException)
        {
            number = 0;
            return false;
        }
    }
}
