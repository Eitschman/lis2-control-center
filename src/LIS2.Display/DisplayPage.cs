namespace LIS2.Display;

public sealed record DisplayPage(
    string Id,
    string Name,
    string Line1Template,
    string Line2Template,
    TimeSpan Duration,
    int Priority = 0,
    string? VisibilityExpression = null,
    DisplayOverflowMode Line1OverflowMode = DisplayOverflowMode.PingPong,
    DisplayOverflowMode Line2OverflowMode = DisplayOverflowMode.PingPong,
    TimeSpan? ScrollStepInterval = null,
    TimeSpan? ScrollEdgePause = null);
