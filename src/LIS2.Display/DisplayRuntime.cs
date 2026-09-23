namespace LIS2.Display;

public sealed class DisplayRuntime
{
    private readonly TemplateRenderer _renderer;
    private readonly PageScheduler _scheduler;
    private readonly EventQueue _events;

    public DisplayRuntime(
        TemplateRenderer renderer,
        PageScheduler scheduler,
        EventQueue events)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public DisplayFrame? RenderNext(
        IReadOnlyDictionary<string, object?> values,
        DateTimeOffset now)
    {
        var activeEvent = _events.Peek(now);
        if (activeEvent is not null)
            return activeEvent.Frame;

        var page = _scheduler.Next();
        if (page is null)
            return null;

        return _renderer.RenderFrame(
            page.Line1Template,
            page.Line2Template,
            values);
    }
}
