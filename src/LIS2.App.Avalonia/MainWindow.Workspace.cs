using Avalonia.Controls;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly PortableWorkspaceStore _workspaceStore = new();
    private bool _workspaceLoaded;

    private void LoadPortableWorkspace()
    {
        try
        {
            var saved = _workspaceStore.Load();
            this.FindControl<TextBox>("HaUrl")!.Text = saved.HomeAssistantUrl;
            if (saved.Pages.Count > 0)
            {
                _pages.ImportJson(System.Text.Json.JsonSerializer.Serialize(saved.Pages));
                RefreshPageList(0);
            }
            _workspaceLoaded = true;
        }
        catch (Exception ex)
        {
            SetVirtualStatus($"Workspace load failed: {ex.Message}");
            _workspaceLoaded = true;
        }
    }

    private async Task SavePortableWorkspaceAsync()
    {
        if (!_workspaceLoaded) return;
        try
        {
            await _workspaceStore.SaveAsync(new PortableWorkspace
            {
                Pages = System.Text.Json.JsonSerializer.Deserialize<List<PageDefinition>>(_pages.ExportJson())
                    ?? new List<PageDefinition>(),
                HomeAssistantUrl = this.FindControl<TextBox>("HaUrl")!.Text ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            SetVirtualStatus($"Workspace save failed: {ex.Message}");
        }
    }
}
