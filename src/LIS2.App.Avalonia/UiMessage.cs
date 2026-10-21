using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace LIS2.App.Avalonia;

/// <summary>Bewahrt Textvorlage und Nutzdaten getrennt, damit Statusmeldungen beim Sprachwechsel neu formatiert werden.</summary>
[InterpolatedStringHandler]
internal sealed class UiMessage
{
    private readonly StringBuilder _template = new();
    private readonly List<object?> _arguments = new();
    private readonly bool _composite;

    public UiMessage(int literalLength, int formattedCount) => _composite = true;
    private UiMessage(string text) => _template.Append(text);
    public static implicit operator UiMessage(string text) => new(text);

    internal static UiMessage Join(string separator, IEnumerable<UiMessage> messages)
    {
        var result = new UiMessage(0, 0);
        foreach (var message in messages)
        {
            if (result._arguments.Count > 0) result.AppendLiteral(separator);
            result.AppendFormatted(message);
        }
        return result;
    }

    public void AppendLiteral(string text) => _template.Append(text.Replace("{", "{{").Replace("}", "}}"));
    public void AppendFormatted<T>(T value, string? format = null) => AppendFormatted(value, 0, format);
    public void AppendFormatted<T>(T value, int alignment, string? format = null)
    {
        _template.Append('{').Append(_arguments.Count);
        if (alignment != 0) _template.Append(',').Append(alignment);
        if (format is not null) _template.Append(':').Append(format);
        _template.Append('}');
        _arguments.Add(value);
    }

    internal string Render(string language)
    {
        var text = UiTranslations.Translate(_template.ToString(), language);
        var arguments = _arguments.Select(value => value is UiMessage message ? message.Render(language) : value).ToArray();
        return _composite ? string.Format(CultureInfo.CurrentCulture, text, arguments) : text;
    }
}
