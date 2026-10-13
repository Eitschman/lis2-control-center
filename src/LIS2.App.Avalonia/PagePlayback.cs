using LIS2.Display;

namespace LIS2.App.Avalonia;

/// <summary>UI-independent page sequence playback using the original LIS2 scheduler.</summary>
public sealed class PagePlayback
{
    private readonly PageScheduler _scheduler = new();
    private readonly VisibilityEvaluator _visibility = new();
    private readonly TemplateRenderer _renderer = new();
    private DisplayPage? _current;
    private DateTimeOffset _nextSwitch;
    private DateTimeOffset _nextRefresh;
    private DisplayFrame? _lastFrame;

    public string? CurrentPageId => _current?.Id;

    public void Reload(IEnumerable<PageDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        _scheduler.ReplacePages(definitions.Where(p => p.Enabled).Select(p => new DisplayPage(
            p.Id, p.Name, p.Line1Template, p.Line2Template,
            TimeSpan.FromSeconds(p.DurationSeconds), p.Priority,
            p.VisibilityExpression, ParseOverflow(p.Line1OverflowMode),
            ParseOverflow(p.Line2OverflowMode),
            TimeSpan.FromMilliseconds(p.ScrollStepMilliseconds),
            TimeSpan.FromMilliseconds(p.ScrollEdgePauseMilliseconds),
            TimeSpan.FromMilliseconds(p.RefreshIntervalMilliseconds))));
        _current = null;
        _nextSwitch = default;
        _nextRefresh = default;
        _lastFrame = null;
    }

    public DisplayFrame? Tick(DateTimeOffset now, IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var previous = _current;
        if (_current is null || now >= _nextSwitch ||
            !_visibility.IsVisible(_current.VisibilityExpression, values))
        {
            _current = _scheduler.Next(p => _visibility.IsVisible(p.VisibilityExpression, values));
            if (_current is null)
            {
                _lastFrame = null;
                return null;
            }
            _nextSwitch = now + _current.Duration;
            if (!ReferenceEquals(previous, _current))
                _nextRefresh = default;
        }
        if (_lastFrame is not null && now < _nextRefresh)
            return _lastFrame;
        _lastFrame = _renderer.RenderFrame(_current.Line1Template, _current.Line2Template, values);
        _nextRefresh = now + (_current.RefreshInterval ?? TimeSpan.FromSeconds(1));
        return _lastFrame;
    }

    private static DisplayOverflowMode ParseOverflow(string? value) =>
        Enum.TryParse<DisplayOverflowMode>(value, true, out var mode)
            ? mode : DisplayOverflowMode.PingPong;
}
