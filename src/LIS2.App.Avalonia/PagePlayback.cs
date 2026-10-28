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
    private string? _requestedPageId;
    private int _requestedDirection;
    private DateTimeOffset? _pausedAt;

    public bool RotationPaused => _pausedAt.HasValue;

    /// <summary>Nur die Rotation pausiert; Datenaktualisierung und Laufschrift laufen weiter.</summary>
    public void SetRotationPaused(bool paused, DateTimeOffset now)
    {
        if (paused == RotationPaused) return;
        if (paused) _pausedAt = now;
        else
        {
            if (_current is not null) _nextSwitch += now - _pausedAt!.Value;
            _pausedAt = null;
        }
    }

    public void RequestPage(string? pageId, int direction = 0)
    {
        _requestedPageId = pageId;
        _requestedDirection = direction;
    }

    public string? CurrentPageId => _current?.Id;
    public TimeSpan NextDelay { get; private set; } = TimeSpan.FromMilliseconds(250);

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
        _requestedPageId = null;
        _requestedDirection = 0;
        _nextSwitch = default;
        _nextRefresh = default;
        _lastFrame = null;
        NextDelay = TimeSpan.FromMilliseconds(250);
        _renderedLine1 = string.Empty;
        _renderedLine2 = string.Empty;
        _line1Scroller.Reset(string.Empty, DateTimeOffset.MinValue);
        _line2Scroller.Reset(string.Empty, DateTimeOffset.MinValue);
    }

    public DisplayFrame? Tick(DateTimeOffset now, IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var previous = _current;
        var requested = _requestedPageId is not null
            ? _scheduler.Select(_requestedPageId, page => _visibility.IsVisible(page.VisibilityExpression, values))
            : _requestedDirection != 0
                ? _scheduler.Move(_requestedDirection, page => _visibility.IsVisible(page.VisibilityExpression, values)) : null;
        _requestedPageId = null;
        _requestedDirection = 0;
        if (requested is not null)
        {
            _current = requested;
            _nextSwitch = now + _current.Duration;
            if (RotationPaused) _pausedAt = now;
            _nextRefresh = default;
        }
        else if (_current is null || !RotationPaused && now >= _nextSwitch ||
            !_visibility.IsVisible(_current.VisibilityExpression, values))
        {
            _current = _scheduler.Next(p => _visibility.IsVisible(p.VisibilityExpression, values));
            if (_current is null)
            {
                _lastFrame = null;
                NextDelay = TimeSpan.FromMilliseconds(250);
                return null;
            }
            _nextSwitch = now + _current.Duration;
            if (RotationPaused) _pausedAt = now;
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
        // A bounded visibility poll retains immediate-ish reaction to external source changes.
        var next = TimeSpan.FromSeconds(1);
        var refresh = _nextRefresh - now;
        var rotate = _nextSwitch - now;
        if (refresh < next) next = refresh;
        if (!RotationPaused && rotate < next) next = rotate;
        var first = _line1Scroller.TimeUntilNextChange(now, _current.Line1OverflowMode);
        var second = _line2Scroller.TimeUntilNextChange(now, _current.Line2OverflowMode);
        if (first != Timeout.InfiniteTimeSpan && first < next) next = first;
        if (second != Timeout.InfiniteTimeSpan && second < next) next = second;
        NextDelay = next < TimeSpan.FromMilliseconds(1) ? TimeSpan.FromMilliseconds(1) : next;
        return _lastFrame;
    }

    private static DisplayOverflowMode ParseOverflow(string? value) =>
        Enum.TryParse<DisplayOverflowMode>(value, true, out var mode)
            ? mode : DisplayOverflowMode.PingPong;
}
