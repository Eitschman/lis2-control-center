using System.Globalization;

namespace LIS2.Display;

public sealed class VisibilityEvaluator
{
    private static readonly string[] Operators =
        [">=", "<=", "!=", "=", ">", "<"];

    public bool IsVisible(
        string? expression,
        IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (string.IsNullOrWhiteSpace(expression))
            return true;

        if (!TryParse(expression, out var key, out var @operator, out var expected))
            return false;

        if (!values.TryGetValue(key, out var actual) || actual is null)
            return false;

        var actualText = Convert.ToString(actual, CultureInfo.InvariantCulture) ?? string.Empty;

        if (@operator is "=" or "!=")
        {
            var equals = string.Equals(
                actualText,
                expected,
                StringComparison.OrdinalIgnoreCase);

            return @operator == "=" ? equals : !equals;
        }

        if (!TryNumber(actual, out var actualNumber) ||
            !double.TryParse(
                expected,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out var expectedNumber))
        {
            return false;
        }

        return @operator switch
        {
            ">" => actualNumber > expectedNumber,
            ">=" => actualNumber >= expectedNumber,
            "<" => actualNumber < expectedNumber,
            "<=" => actualNumber <= expectedNumber,
            _ => false
        };
    }

    public static bool TryParse(
        string? expression,
        out string key,
        out string @operator,
        out string expected)
    {
        key = string.Empty;
        @operator = string.Empty;
        expected = string.Empty;

        if (string.IsNullOrWhiteSpace(expression))
            return false;

        foreach (var candidate in Operators)
        {
            var index = expression.IndexOf(candidate, StringComparison.Ordinal);
            if (index <= 0)
                continue;

            var valueStart = index + candidate.Length;
            if (valueStart >= expression.Length)
                return false;

            key = expression[..index].Trim();
            @operator = candidate;
            expected = expression[valueStart..].Trim();
            return key.Length > 0 && expected.Length > 0;
        }

        return false;
    }

    private static bool TryNumber(object value, out double number)
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
