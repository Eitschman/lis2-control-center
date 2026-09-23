namespace LIS2.Display;

public sealed class DisplayRuntime
{
    private readonly TemplateRenderer _renderer;
    private readonly PageScheduler _scheduler;
    private readonly EventQueue _events;
    private readonly VisibilityEvaluator _visibility = new();

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

        var page = _scheduler.Next(candidate =>
            _visibility.IsVisible(candidate.VisibilityExpression, values));

        if (page is null)
            return null;

        SuggestedDuration = page.Duration <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(1)
            : page.Duration;

        return _renderer.RenderFrame(
            page.Line1Template,
            page.Line2Template,
            values);
    }
}
