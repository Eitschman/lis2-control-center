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
    private readonly PingPongScroller _line1Scroller = new();
    private readonly PingPongScroller _line2Scroller = new();
    private string _renderedLine1 = string.Empty;
    private string _renderedLine2 = string.Empty;

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
        _renderedLine1 = string.Empty;
        _renderedLine2 = string.Empty;
        _line1Scroller.Reset(string.Empty, DateTimeOffset.MinValue);
        _line2Scroller.Reset(string.Empty, DateTimeOffset.MinValue);
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
        if (!ReferenceEquals(previous, _current))
        {
            _renderedLine1 = string.Empty;
            _renderedLine2 = string.Empty;
            _line1Scroller.Reset(string.Empty, now);
            _line2Scroller.Reset(string.Empty, now);
        }

        if (_lastFrame is null || now >= _nextRefresh || !ReferenceEquals(previous, _current))
        {
            _renderedLine1 = _renderer.Render(_current.Line1Template, values);
            _renderedLine2 = _renderer.Render(_current.Line2Template, values);
            _nextRefresh = now + (_current.RefreshInterval ?? TimeSpan.FromSeconds(1));
        }

        _lastFrame = new DisplayFrame(
            _line1Scroller.Render(_renderedLine1, now, _current.Line1OverflowMode,
                _current.ScrollStepInterval, _current.ScrollEdgePause),
            _line2Scroller.Render(_renderedLine2, now, _current.Line2OverflowMode,
                _current.ScrollStepInterval, _current.ScrollEdgePause));
        return _lastFrame;
    }

    private static DisplayOverflowMode ParseOverflow(string? value) =>
        Enum.TryParse<DisplayOverflowMode>(value, true, out var mode)
            ? mode : DisplayOverflowMode.PingPong;
}
