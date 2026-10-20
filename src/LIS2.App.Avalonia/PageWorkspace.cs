using System.Text.Json;
using LIS2.Display;

namespace LIS2.App.Avalonia;

public sealed class PageWorkspace
{
    private readonly List<PageDefinition> _pages = new();
    private readonly TemplateRenderer _renderer = new();

    public IReadOnlyList<PageDefinition> Pages => _pages;

    public PageDefinition Add()
    {
        var page = new PageDefinition { Name = $"Page {_pages.Count + 1}" };
        _pages.Add(page);
        return page;
    }

    public PageDefinition Duplicate(int index)
    {
        var original = Get(index);
        var copy = Clone(original);
        copy.Id = Guid.NewGuid().ToString("N");
        copy.Name += " (copy)";
        _pages.Insert(index + 1, copy);
        return copy;
    }

    public void Delete(int index) => _pages.RemoveAt(CheckedIndex(index));

    public int Move(int index, int offset)
    {
        index = CheckedIndex(index);
        var target = Math.Clamp(index + offset, 0, _pages.Count - 1);
        var page = _pages[index];
        _pages.RemoveAt(index);
        _pages.Insert(target, page);
        return target;
    }

    public PageDefinition Get(int index) => _pages[CheckedIndex(index)];

    public DisplayFrame Preview(int index, IReadOnlyDictionary<string, object?> values)
    {
        var page = Get(index);
        return _renderer.RenderFrame(page.Line1Template, page.Line2Template, values);
    }

    public string ExportJson() => JsonSerializer.Serialize(_pages,
        new JsonSerializerOptions { WriteIndented = true });

    public void ImportJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var pages = JsonSerializer.Deserialize<List<PageDefinition>>(json)
            ?? throw new InvalidOperationException("Invalid page configuration.");
        if (pages.Any(p => p is null || string.IsNullOrWhiteSpace(p.Id) || p.DurationSeconds <= 0 ||
                           p.RefreshIntervalMilliseconds is < 100 or > 60000 ||
                           p.ScrollStepMilliseconds <= 0 || p.ScrollEdgePauseMilliseconds < 0 ||
                           (!string.IsNullOrWhiteSpace(p.VisibilityExpression) &&
                            !VisibilityEvaluator.TryParse(p.VisibilityExpression, out _, out _, out _))))
            throw new InvalidOperationException("Page configuration contains invalid timing or identifiers.");
        if (pages.Select(p => p.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != pages.Count)
            throw new InvalidOperationException("Page configuration contains duplicate page identifiers.");
        if (pages.Any(p => p.Line1Template is null || p.Line2Template is null || p.Name is null))
            throw new InvalidOperationException("Page configuration contains null page text fields.");
        _pages.Clear();
        _pages.AddRange(pages);
    }

    private int CheckedIndex(int index) =>
        index >= 0 && index < _pages.Count ? index : throw new ArgumentOutOfRangeException(nameof(index));

    private static PageDefinition Clone(PageDefinition p) => new()
    {
        Id = p.Id, Name = p.Name, Line1Template = p.Line1Template,
        Line2Template = p.Line2Template, DurationSeconds = p.DurationSeconds,
        RefreshIntervalMilliseconds = p.RefreshIntervalMilliseconds,
        Enabled = p.Enabled, Priority = p.Priority,
        VisibilityExpression = p.VisibilityExpression,
        Line1OverflowMode = p.Line1OverflowMode,
        Line2OverflowMode = p.Line2OverflowMode,
        ScrollStepMilliseconds = p.ScrollStepMilliseconds,
        ScrollEdgePauseMilliseconds = p.ScrollEdgePauseMilliseconds
    };
}
