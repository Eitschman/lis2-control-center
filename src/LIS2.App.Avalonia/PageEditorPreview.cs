using LIS2.Display;

namespace LIS2.App.Avalonia;

/// <summary>
/// Renders the currently edited page using the same overflow and template pipeline
/// as live playback, without changing the persisted page or advancing the scheduler.
/// </summary>
public static class PageEditorPreview
{
    public static DisplayFrame Render(
        string line1, string line2, string overflow1, string overflow2,
        int scrollStepMilliseconds, int edgePauseMilliseconds,
        IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var playback = new PagePlayback();
        playback.Reload([new PageDefinition
        {
            Id = "editor-preview",
            Line1Template = line1 ?? "",
            Line2Template = line2 ?? "",
            Line1OverflowMode = overflow1,
            Line2OverflowMode = overflow2,
            ScrollStepMilliseconds = Math.Max(1, scrollStepMilliseconds),
            ScrollEdgePauseMilliseconds = Math.Max(0, edgePauseMilliseconds),
            DurationSeconds = 5
        }]);
        return playback.Tick(DateTimeOffset.UnixEpoch, values)
            ?? throw new InvalidOperationException("Editor preview produced no frame.");
    }
}
