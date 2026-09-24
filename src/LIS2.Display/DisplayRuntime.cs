namespace LIS2.Display;

public sealed class DisplayRuntime
{
    private readonly TemplateRenderer _renderer;
    private readonly PageScheduler _scheduler;
    private readonly EventQueue _events;
    private readonly VisibilityEvaluator _visibility = new();
    private readonly PingPongScroller _line1Scroller = new();
    private readonly PingPongScroller _line2Scroller = new();

    private DisplayPage? _activePage;
    private DateTimeOffset _activePageUntil = DateTimeOffset.MinValue;

    public DisplayRuntime(
        TemplateRenderer renderer,
        PageScheduler scheduler,
        EventQueue events)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public TimeSpan SuggestedDuration { get; private set; } = TimeSpan.FromSeconds(5);

    public void ResetPageSelection()
    {
        _activePage = null;
        _activePageUntil = DateTimeOffset.MinValue;
    }

    public DisplayFrame? RenderNext(
        IReadOnlyDictionary<string, object?> values,
        DateTimeOffset now)
    {
        var activeEvent = _events.Peek(now);
        if (activeEvent is not null)
        {
            SuggestedDuration = TimeSpan.FromSeconds(1);
            return activeEvent.Frame;
        }

        if (_activePage is null ||
            now >= _activePageUntil ||
            !_visibility.IsVisible(_activePage.VisibilityExpression, values))
        {
            SelectNextPage(values, now);
        }

        if (_activePage is null)
            return null;

        var rawLine1 = _renderer.Render(_activePage.Line1Template, values);
        var rawLine2 = _renderer.Render(_activePage.Line2Template, values);

        var step = _activePage.ScrollStepInterval;
        var pause = _activePage.ScrollEdgePause;

        var line1 = _line1Scroller.Render(
            rawLine1,
            now,
            _activePage.Line1OverflowMode,
            step,
            pause);
        var line2 = _line2Scroller.Render(
            rawLine2,
            now,
            _activePage.Line2OverflowMode,
            step,
            pause);

        SuggestedDuration = CalculateNextDelay(now);

        return new DisplayFrame(line1, line2);
    }

    private void SelectNextPage(
        IReadOnlyDictionary<string, object?> values,
        DateTimeOffset now)
    {
        var next = _scheduler.Next(candidate =>
            _visibility.IsVisible(candidate.VisibilityExpression, values));

        if (next is null)
        {
            _activePage = null;
            _activePageUntil = DateTimeOffset.MinValue;
            return;
        }

        var pageChanged =
            _activePage is null ||
            !string.Equals(_activePage.Id, next.Id, StringComparison.OrdinalIgnoreCase);

        _activePage = next;
        _activePageUntil = now + NormalizePageDuration(next.Duration);

        if (pageChanged)
        {
            _line1Scroller.Reset(
                _renderer.Render(next.Line1Template, values),
                now,
                next.ScrollEdgePause);
            _line2Scroller.Reset(
                _renderer.Render(next.Line2Template, values),
                now,
                next.ScrollEdgePause);
        }
    }

    private TimeSpan CalculateNextDelay(DateTimeOffset now)
    {
        var untilPageChange = _activePageUntil - now;
        if (untilPageChange <= TimeSpan.Zero)
            return TimeSpan.FromMilliseconds(1);

        var nextDelay = untilPageChange;

        var line1Delay = _line1Scroller.TimeUntilNextChange(
            now,
            _activePage?.Line1OverflowMode ?? DisplayOverflowMode.PingPong);
        if (line1Delay != Timeout.InfiniteTimeSpan && line1Delay < nextDelay)
            nextDelay = line1Delay;

        var line2Delay = _line2Scroller.TimeUntilNextChange(
            now,
            _activePage?.Line2OverflowMode ?? DisplayOverflowMode.PingPong);
        if (line2Delay != Timeout.InfiniteTimeSpan && line2Delay < nextDelay)
            nextDelay = line2Delay;

        return nextDelay < TimeSpan.FromMilliseconds(1)
            ? TimeSpan.FromMilliseconds(1)
            : nextDelay;
    }

    private static TimeSpan NormalizePageDuration(TimeSpan duration) =>
        duration <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(1)
            : duration;
}
