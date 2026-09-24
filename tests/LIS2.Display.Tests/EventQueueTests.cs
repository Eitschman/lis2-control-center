using LIS2.Display;

namespace LIS2.Display.Tests;

public sealed class EventQueueTests
{
    [Fact]
    public void Snapshot_SortsByPriorityThenExpiry()
    {
        var queue = new EventQueue();
        var now = DateTimeOffset.UtcNow;

        queue.Add(new DisplayEvent(
            "info",
            DisplayFrame.Create("Info", "Low"),
            10,
            now.AddSeconds(20)));

        queue.Add(new DisplayEvent(
            "warning-late",
            DisplayFrame.Create("Warning", "Later"),
            100,
            now.AddSeconds(30)));

        queue.Add(new DisplayEvent(
            "warning-early",
            DisplayFrame.Create("Warning", "Earlier"),
            100,
            now.AddSeconds(10)));

        var events = queue.Snapshot(now);

        Assert.Equal(
            new[] { "warning-early", "warning-late", "info" },
            events.Select(item => item.Id));
    }

    [Fact]
    public void Add_ReplacesExistingEventWithSameId()
    {
        var queue = new EventQueue();
        var now = DateTimeOffset.UtcNow;

        queue.Add(new DisplayEvent(
            "build",
            DisplayFrame.Create("Old", "Value"),
            10,
            now.AddSeconds(20)));

        queue.Add(new DisplayEvent(
            "build",
            DisplayFrame.Create("New", "Value"),
            100,
            now.AddSeconds(30)));

        var events = queue.Snapshot(now);

        Assert.Single(events);
        Assert.Equal(100, events[0].Priority);
        Assert.StartsWith("New", events[0].Frame.Line1);
    }

    [Fact]
    public void Snapshot_RemovesExpiredEvents()
    {
        var queue = new EventQueue();
        var now = DateTimeOffset.UtcNow;

        queue.Add(new DisplayEvent(
            "expired",
            DisplayFrame.Create("Old", "Event"),
            200,
            now.AddSeconds(-1)));

        queue.Add(new DisplayEvent(
            "active",
            DisplayFrame.Create("Current", "Event"),
            10,
            now.AddSeconds(5)));

        var events = queue.Snapshot(now);

        Assert.Single(events);
        Assert.Equal("active", events[0].Id);
    }

    [Fact]
    public void Remove_AndClear_ModifyQueue()
    {
        var queue = new EventQueue();
        var now = DateTimeOffset.UtcNow;

        queue.Add(new DisplayEvent(
            "one",
            DisplayFrame.Create("One", ""),
            10,
            now.AddSeconds(10)));

        queue.Add(new DisplayEvent(
            "two",
            DisplayFrame.Create("Two", ""),
            10,
            now.AddSeconds(10)));

        Assert.True(queue.Remove("one"));
        Assert.False(queue.Remove("missing"));
        Assert.Single(queue.Snapshot(now));

        queue.Clear();

        Assert.Empty(queue.Snapshot(now));
    }
}
