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

    public DisplayPage? Next(Func<DisplayPage, bool>? predicate = null) => Move(1, predicate);

    /// <summary>Manuelles Blättern berücksichtigt dieselbe Sichtbarkeit wie die automatische Folge.</summary>
    public DisplayPage? Move(int direction, Func<DisplayPage, bool>? predicate = null)
    {
        if (_pages.Count == 0)
            return null;

        predicate ??= static _ => true;
        if (_index < 0 && direction < 0) _index = 0;

        for (var attempt = 0; attempt < _pages.Count; attempt++)
        {
            _index = (_index + (direction < 0 ? _pages.Count - 1 : 1)) % _pages.Count;
            var page = _pages[_index];
            if (predicate(page))
                return page;
        }

        return null;
    }

    public DisplayPage? Select(string id, Func<DisplayPage, bool> predicate)
    {
        var index = _pages.FindIndex(page => page.Id == id && predicate(page));
        if (index < 0) return null;
        _index = index;
        return _pages[index];
    }
}
