namespace LIS2.App;

public sealed record EventQueueRow(
    string Id,
    string Line1,
    string Line2,
    int Priority,
    string PriorityName,
    DateTimeOffset ExpiresAt,
    string Remaining)
{
    public string DisplayName => $"{PriorityName}: {Line1.Trim()} / {Line2.Trim()}";
}
