namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly Dictionary<string, int> _editorSaveVersions = new();

    /// <summary>Ältere Schreibabschlüsse dürfen neuere, noch ungespeicherte Eingaben nicht bestätigen.</summary>
    private void MarkEditorChanged(string status)
    {
        _editorSaveVersions[status] = _editorSaveVersions.GetValueOrDefault(status) + 1;
        SetUiText(status, "Changed");
    }

    private async Task<bool> PersistEditorAsync(string status)
    {
        var version = _editorSaveVersions[status] = _editorSaveVersions.GetValueOrDefault(status) + 1;
        SetUiText(status, "Saving…");
        try
        {
            await SavePortableWorkspaceAsync(true);
            if (_editorSaveVersions[status] == version) SetUiText(status, "Saved");
            return true;
        }
        catch (Exception exception)
        {
            if (_editorSaveVersions[status] == version)
                SetUiText(status, $"Save failed: {(UiMessage)exception.Message}");
            return false;
        }
    }
}
