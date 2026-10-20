using LIS2.Display;

namespace LIS2.App.Avalonia;

/// <summary>
/// Stateful editor-only playback. Drafts are never written to the workspace or
/// the active device sequence; time is supplied by the caller for deterministic tests.
/// </summary>
public sealed class PageEditorPreview
{
    private readonly PagePlayback _playback = new();
    private bool _configured;

    public TimeSpan NextDelay => _playback.NextDelay;

    public void Configure(string line1, string line2, string overflow1, string overflow2,
        int scrollStepMilliseconds, int edgePauseMilliseconds)
    {
        _playback.Reload([new PageDefinition
        {
            Id = "editor-preview",
            Line1Template = line1 ?? "",
            Line2Template = line2 ?? "",
            Line1OverflowMode = overflow1,
            Line2OverflowMode = overflow2,
            ScrollStepMilliseconds = Math.Max(1, scrollStepMilliseconds),
            ScrollEdgePauseMilliseconds = Math.Max(0, edgePauseMilliseconds),
            RefreshIntervalMilliseconds = 250,
            DurationSeconds = 86400
        }]);
        _configured = true;
    }

    public DisplayFrame Tick(DateTimeOffset now, IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (!_configured)
            throw new InvalidOperationException("Configure the editor preview before ticking.");
        return _playback.Tick(now, values)
            ?? throw new InvalidOperationException("Editor preview produced no frame.");
    }

    /// <summary>Compatibility helper for a deterministic initial frame.</summary>
    public static DisplayFrame Render(string line1, string line2, string overflow1, string overflow2,
        int scrollStepMilliseconds, int edgePauseMilliseconds,
        IReadOnlyDictionary<string, object?> values)
    {
        var preview = new PageEditorPreview();
        preview.Configure(line1, line2, overflow1, overflow2,
            scrollStepMilliseconds, edgePauseMilliseconds);
        return preview.Tick(DateTimeOffset.UnixEpoch, values);
    }
}
