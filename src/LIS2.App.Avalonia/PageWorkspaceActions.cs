using System.Text.Json;

namespace LIS2.App.Avalonia;

/// <summary>Hält Quellenaktionen, Editorentwurf und laufende Seitenfolgen zusammen.</summary>
internal sealed class PageWorkspaceActions(
    PageWorkspace workspace,
    Func<bool> commitEditor,
    Action<int> selectPage,
    Func<Task> reloadSequences,
    Func<Task> saveWorkspace,
    Action openEditor)
{
    /// <summary>Wechselt erst nach erfolgreicher Entwurfsvalidierung zur neuen Quellenseite.</summary>
    public async Task<bool> AddSourcePageAsync(Action<PageDefinition> configure)
    {
        if (!commitEditor()) return false;
        configure(workspace.Add());
        selectPage(workspace.Pages.Count - 1);
        await reloadSequences();
        await saveWorkspace();
        openEditor();
        return true;
    }

    /// <summary>Übernimmt auch eine bewusst leere Seitenliste; Import validiert vor dem Ersetzen.</summary>
    public static void RestorePages(PageWorkspace workspace, PortableWorkspace saved) =>
        workspace.ImportJson(JsonSerializer.Serialize(saved.Pages));
}
