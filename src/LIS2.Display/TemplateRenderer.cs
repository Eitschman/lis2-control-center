using System.Text.RegularExpressions;

namespace LIS2.Display;

public sealed partial class TemplateRenderer
{
    [GeneratedRegex(@"{(?<name>[A-Za-z0-9_.-]+)}", RegexOptions.CultureInvariant)]
    private static partial Regex VariablePattern();

    public string Render(string template, IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);

        return VariablePattern().Replace(template, match =>
        {
            var name = match.Groups["name"].Value;
            return values.TryGetValue(name, out var value)
                ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
                : string.Empty;
        });
    }

    public DisplayFrame RenderFrame(
        string line1Template,
        string line2Template,
        IReadOnlyDictionary<string, object?> values) =>
        DisplayFrame.Create(
            Render(line1Template, values),
            Render(line2Template, values));
}
