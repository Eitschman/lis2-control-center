namespace LIS2.Display;

public sealed class VisibilityEvaluator
{
    public bool IsVisible(
        string? expression,
        IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (string.IsNullOrWhiteSpace(expression))
            return true;

        var separator = expression.IndexOf('=');
        if (separator <= 0 || separator == expression.Length - 1)
            return false;

        var key = expression[..separator].Trim();
        var expected = expression[(separator + 1)..].Trim();

        if (!values.TryGetValue(key, out var actual))
            return false;

        var actualText = Convert.ToString(
            actual,
            System.Globalization.CultureInfo.InvariantCulture);

        return string.Equals(
            actualText,
            expected,
            StringComparison.OrdinalIgnoreCase);
    }
}
