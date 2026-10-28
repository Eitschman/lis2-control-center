using System.Globalization;
using System.Text.RegularExpressions;

namespace LIS2.Display;

public static partial class SourceValueFormatter
{
    [GeneratedRegex(@"\A[+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?\z", RegexOptions.CultureInvariant)]
    private static partial Regex NumericTextPattern();

    /// <summary>Typisierte Zahlen folgen der Region; Metadatentexte werden nicht als Zahlen interpretiert.</summary>
    public static string Format(object? value) => Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;

    /// <summary>Numerische Protokolltexte ohne Rundung darstellen, etwa HA-Zustände und bekannte Sensorwerte.</summary>
    public static string FormatNumeric(object? value) => value is string text && NumericTextPattern().IsMatch(text)
        ? text.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal)
        : Format(value);

    /// <summary>HA-Zustände anhand ihrer mitgelieferten Einheit erkennen, einschließlich leerer Einheit und Alias.</summary>
    public static string Format(object? value, string? key, IReadOnlyDictionary<string, object?> values) =>
        key?.StartsWith("HA.", StringComparison.OrdinalIgnoreCase) == true && values.ContainsKey(key + ".Unit")
            ? FormatNumeric(value) : Format(value);
}
