namespace LIS2.Display;

public sealed class PageScheduler
{
    private readonly List<DisplayPage> _pages = new();
    private int _index = -1;

    public IReadOnlyList<DisplayPage> Pages => _pages;

    public void ReplacePages(IEnumerable<DisplayPage> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);
        _pages.Clear();
        _pages.AddRange(pages);
        _index = -1;
    }

    public DisplayPage? Next(Func<DisplayPage, bool>? predicate = null)
    {
        if (_pages.Count == 0)
            return null;

        predicate ??= static _ => true;

        for (var attempt = 0; attempt < _pages.Count; attempt++)
        {
            _index = (_index + 1) % _pages.Count;
            var page = _pages[_index];
            if (predicate(page))
                return page;
        }

        return null;
    }
}
