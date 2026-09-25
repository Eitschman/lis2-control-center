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
        var parts = expression
            .Split('|', StringSplitOptions.TrimEntries);

        if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            return string.Empty;

        var name = parts[0];
        values.TryGetValue(name, out var rawValue);

        var unavailable =
            rawValue is null ||
            string.Equals(
                Convert.ToString(rawValue, CultureInfo.InvariantCulture),
                "unknown",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                Convert.ToString(rawValue, CultureInfo.InvariantCulture),
                "unavailable",
                StringComparison.OrdinalIgnoreCase);

        var fallback = parts
            .Skip(1)
            .Select(ParseOperation)
            .FirstOrDefault(operation =>
                string.Equals(
                    operation.Name,
                    "fallback",
                    StringComparison.OrdinalIgnoreCase))
            .Argument;

        if (unavailable)
            return fallback ?? string.Empty;

        object? current = rawValue;

        foreach (var part in parts.Skip(1))
        {
            var operation = ParseOperation(part);

            switch (operation.Name.ToLowerInvariant())
            {
                case "fallback":
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
                    current = Convert.ToString(current, CultureInfo.CurrentCulture)?
                        .ToUpper(CultureInfo.CurrentCulture);
                    break;

                case "lower":
                    current = Convert.ToString(current, CultureInfo.CurrentCulture)?
                        .ToLower(CultureInfo.CurrentCulture);
                    break;

                case "prefix":
                    current = (operation.Argument ?? string.Empty) +
                              (Convert.ToString(current, CultureInfo.CurrentCulture) ?? string.Empty);
                    break;

                case "suffix":
                    current = (Convert.ToString(current, CultureInfo.CurrentCulture) ?? string.Empty) +
                              (operation.Argument ?? string.Empty);
                    break;
            }
        }

        var result = Convert.ToString(current, CultureInfo.CurrentCulture) ?? string.Empty;
        return string.IsNullOrEmpty(result) ? fallback ?? string.Empty : result;
    }

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
