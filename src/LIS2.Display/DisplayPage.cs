namespace LIS2.Display;

public sealed record DisplayPage(
    string Id,
    string Name,
    string Line1Template,
    string Line2Template,
    TimeSpan Duration,
    int Priority = 0,
    string? VisibilityExpression = null);
