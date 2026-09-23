namespace LIS2.Display;

public sealed class EventQueue
{
    private readonly List<DisplayEvent> _events = new();

    public void Add(DisplayEvent displayEvent)
    {
        ArgumentNullException.ThrowIfNull(displayEvent);
        _events.RemoveAll(item => item.Id == displayEvent.Id);
        _events.Add(displayEvent);
    }

    public DisplayEvent? Peek(DateTimeOffset now)
    {
        _events.RemoveAll(item => item.ExpiresAt <= now);

        return _events
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.ExpiresAt)
            .FirstOrDefault();
    }
}
