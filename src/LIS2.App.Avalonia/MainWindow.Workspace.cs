using Avalonia.Controls;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly PortableWorkspaceStore _workspaceStore = new();
    private bool _workspaceLoaded;
    private List<PortableHaEntityPreference> _haPreferences = new();
    private int _preferredBrightness = 100;
    private string _preferredTheme = "System";
    private string _preferredLanguage = "System";

    private void LoadPortableWorkspace()
    {
        try
        {
            var saved = _workspaceStore.Load();
            ApplyPortableWorkspace(saved);
            _workspaceLoaded = true;
        }
        catch (Exception ex)
        {
            SetVirtualStatus($"Workspace load failed: {ex.Message}");
            _workspaceLoaded = true;
        }
    }

    private void ApplyPortableWorkspace(PortableWorkspace saved)
    {
        this.FindControl<TextBox>("HaUrl")!.Text = saved.HomeAssistantUrl;
        if (saved.CustomGlyphs.Count == 8)
        {
            _glyphs.Replace(saved.CustomGlyphs);
            LoadGlyphEditor();
        }
        _haPreferences = saved.HomeAssistantEntityPreferences.ToList();
        _preferredBrightness = saved.BrightnessPercent;
        _preferredTheme = saved.ThemeMode;
        _preferredLanguage = saved.LanguageMode;
        this.FindControl<CheckBox>("ProgramGlyphsOnConnect")!.IsChecked = saved.ProgramCustomGlyphsOnConnect;
        if (saved.Pages.Count > 0)
        {
            _pages.ImportJson(System.Text.Json.JsonSerializer.Serialize(saved.Pages));
            RefreshPageList(0);
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
                HomeAssistantUrl = this.FindControl<TextBox>("HaUrl")!.Text ?? string.Empty,
                CustomGlyphs = _glyphs.Glyphs.Select(g => new CustomGlyphSettings
                {
                    Name = g.Name, Rows = g.Rows.ToArray()
                }).ToList(),
                HomeAssistantEntityPreferences = _haPreferences.ToList(),
                BrightnessPercent = _preferredBrightness,
                ThemeMode = _preferredTheme,
                LanguageMode = _preferredLanguage,
                ProgramCustomGlyphsOnConnect = this.FindControl<CheckBox>("ProgramGlyphsOnConnect")!.IsChecked == true
            });
        }
        catch (Exception ex)
        {
            SetVirtualStatus($"Workspace save failed: {ex.Message}");
        }
    }
}
