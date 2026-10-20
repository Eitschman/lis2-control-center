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
    private PreviewSettings? _settings;

    private sealed record PreviewSettings(string Line1, string Line2, string Overflow1,
        string Overflow2, int Step, int Pause, int Refresh);

    public TimeSpan NextDelay => _playback.NextDelay;

    public void Configure(string line1, string line2, string overflow1, string overflow2,
        int scrollStepMilliseconds, int edgePauseMilliseconds, int refreshIntervalMilliseconds = 1000)
    {
        var settings = new PreviewSettings(line1 ?? "", line2 ?? "", overflow1 ?? "",
            overflow2 ?? "", Math.Max(1, scrollStepMilliseconds),
            Math.Max(0, edgePauseMilliseconds), Math.Max(1, refreshIntervalMilliseconds));
        if (_configured && settings == _settings) return;
        _settings = settings;
        _playback.Reload([new PageDefinition
        {
            Id = "editor-preview",
            Line1Template = line1 ?? "",
            Line2Template = line2 ?? "",
            Line1OverflowMode = settings.Overflow1,
            Line2OverflowMode = settings.Overflow2,
            ScrollStepMilliseconds = Math.Max(1, scrollStepMilliseconds),
            ScrollEdgePauseMilliseconds = Math.Max(0, edgePauseMilliseconds),
            RefreshIntervalMilliseconds = settings.Refresh,
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
        IReadOnlyDictionary<string, object?> values, int refreshIntervalMilliseconds = 1000)
    {
        var preview = new PageEditorPreview();
        preview.Configure(line1, line2, overflow1, overflow2,
            scrollStepMilliseconds, edgePauseMilliseconds, refreshIntervalMilliseconds);
        return preview.Tick(DateTimeOffset.UnixEpoch, values);
    }
}
