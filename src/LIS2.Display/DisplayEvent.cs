namespace LIS2.Display;

public sealed record DisplayEvent(
    string Id,
    DisplayFrame Frame,
    int Priority,
    DateTimeOffset ExpiresAt);
