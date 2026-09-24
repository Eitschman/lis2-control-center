namespace LIS2.App;

public sealed class PageDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Page";
    public string Line1Template { get; set; } = string.Empty;
    public string Line2Template { get; set; } = string.Empty;
    public int DurationSeconds { get; set; } = 5;
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; }
    public string? VisibilityExpression { get; set; }
    public string Line1OverflowMode { get; set; } = "PingPong";
    public string Line2OverflowMode { get; set; } = "PingPong";
    public int ScrollStepMilliseconds { get; set; } = 300;
    public int ScrollEdgePauseMilliseconds { get; set; } = 900;
}
