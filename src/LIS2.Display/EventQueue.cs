namespace LIS2.Display;

public sealed class EventQueue
{
    private readonly object _gate = new();
    private readonly List<DisplayEvent> _events = new();

    public event EventHandler? Changed;

    public void Add(DisplayEvent displayEvent)
    {
        ArgumentNullException.ThrowIfNull(displayEvent);

        lock (_gate)
        {
            _events.RemoveAll(item =>
                string.Equals(item.Id, displayEvent.Id, StringComparison.OrdinalIgnoreCase));

            _events.Add(displayEvent);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool Remove(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        bool removed;

        lock (_gate)
        {
            removed = _events.RemoveAll(item =>
                string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        if (removed)
            Changed?.Invoke(this, EventArgs.Empty);

        return removed;
    }

    public void Clear()
    {
        bool changed;

        lock (_gate)
        {
            changed = _events.Count > 0;
            _events.Clear();
        }

        if (changed)
            Changed?.Invoke(this, EventArgs.Empty);
    }

    public IReadOnlyList<DisplayEvent> Snapshot(DateTimeOffset now)
    {
        bool removedExpired;
        DisplayEvent[] snapshot;

        lock (_gate)
        {
            removedExpired = RemoveExpiredUnsafe(now);

            snapshot = _events
                .OrderByDescending(item => item.Priority)
                .ThenBy(item => item.ExpiresAt)
                .ThenBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        if (removedExpired)
            Changed?.Invoke(this, EventArgs.Empty);

        return snapshot;
    }

    public DisplayEvent? Peek(DateTimeOffset now) =>
        Snapshot(now).FirstOrDefault();

    private bool RemoveExpiredUnsafe(DateTimeOffset now) =>
        _events.RemoveAll(item => item.ExpiresAt <= now) > 0;
}
