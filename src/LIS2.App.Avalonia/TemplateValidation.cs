using System.Globalization;
using LIS2.Display;

namespace LIS2.App.Avalonia;

internal sealed record TemplateIssue(int Start, int Length, string Code, string Detail, bool IsError);

/// <summary>Prüft dieselbe Pipeline wie der Renderer, ohne Quellenwerte oder Vorlagen zu verändern.</summary>
internal static class TemplateValidation
{
    internal static IReadOnlyList<TemplateIssue> Check(string template, IReadOnlyDictionary<string, object?> values,
        IEnumerable<string> knownKeys)
    {
        var known = new HashSet<string>(knownKeys.Concat(values.Keys), StringComparer.OrdinalIgnoreCase);
        var issues = new List<TemplateIssue>();
        void Add(int start, int length, string code, string detail, bool error = true) =>
            issues.Add(new(start, Math.Max(1, length), code, detail, error));
        void Variable(string key, int start, int length)
        {
            if (key.Length == 0 || key.Any(character => char.IsWhiteSpace(character) || character is '{' or '}' or '|'))
                Add(start, length, "Invalid variable", key);
            else if (!known.Contains(key))
            {
                // Dynamische Schlüssel können ohne verbundenes Zielsystem nicht als Tippfehler bewiesen werden.
                var dynamic = key.StartsWith("Hardware.", StringComparison.OrdinalIgnoreCase) || key.StartsWith("HA.", StringComparison.OrdinalIgnoreCase);
                Add(start, length, key.StartsWith("Glyph.", StringComparison.OrdinalIgnoreCase) ? "Missing glyph" :
                    dynamic ? "Not discovered" : "Unknown variable", key, !dynamic);
            }
            else if (!values.TryGetValue(key, out var value) || TemplateRenderer.IsMissingValue(value))
                Add(start, length, "Currently unavailable", key, false);
        }
        for (var position = 0; position < template.Length; position++)
        {
            if (template[position] == '}') { Add(position, 1, "Unexpected brace", "}"); continue; }
            if (template[position] != '{') continue;
            var end = template.IndexOf('}', position + 1);
            var nested = template.IndexOf('{', position + 1);
            if (end < 0 || nested >= 0 && nested < end)
            {
                Add(position, 1, "Unclosed expression", "{");
                continue;
            }
            var expression = template[(position + 1)..end];
            var parts = expression.Split('|');
            Variable(parts[0].Trim(), position, end - position + 1);
            var offset = position + 1 + parts[0].Length;
            foreach (var part in parts.Skip(1))
            {
                offset++;
                var colon = part.IndexOf(':');
                var operation = (colon < 0 ? part : part[..colon]).Trim().ToLowerInvariant();
                var argument = colon < 0 ? null : part[(colon + 1)..];
                if (operation is "number" or "percent" or "bytes")
                {
                    if (argument is not null && (!int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out var digits) || digits is < 0 or > 6))
                        Add(offset, part.Length, "Decimal places must be 0 to 6", part);
                }
                else if (operation == "fallbackvar") Variable(argument?.Trim() ?? "", offset, part.Length);
                else if (operation is "upper" or "lower")
                {
                    if (argument is not null) Add(offset, part.Length, "Operation takes no argument", part);
                }
                else if (operation is "prefix" or "suffix" or "fallback")
                {
                    if (argument is null) Add(offset, part.Length, "Missing argument separator", part);
                }
                else Add(offset, part.Length, "Unknown operation", part);
                offset += part.Length;
            }
            position = end;
        }
        return issues;
    }
}
