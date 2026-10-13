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
    private PortableFanSettings _fanSettings = new();

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
        // Validate page data before mutating any live UI or workspace state.
        if (saved.Pages.Count > 0)
        {
            var validation = new PageWorkspace();
            validation.ImportJson(System.Text.Json.JsonSerializer.Serialize(saved.Pages));
        }
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
        _fanSettings = PortableFanSettings.Normalize(saved.Fans);
        this.FindControl<CheckBox>("ProgramGlyphsOnConnect")!.IsChecked = saved.ProgramCustomGlyphsOnConnect;
        if (saved.Pages.Count > 0)
        {
            _pages.ImportJson(System.Text.Json.JsonSerializer.Serialize(saved.Pages));
            RefreshPageList(0);
        }
    }

    private PortableWorkspace CapturePortableWorkspace() => new()
    {
        Pages = System.Text.Json.JsonSerializer.Deserialize<List<PageDefinition>>(_pages.ExportJson()) ?? new(),
        HomeAssistantUrl = this.FindControl<TextBox>("HaUrl")!.Text ?? string.Empty,
        CustomGlyphs = _glyphs.Glyphs.Select(g => new CustomGlyphSettings
        {
            Name = g.Name, Rows = g.Rows.ToArray()
        }).ToList(),
        HomeAssistantEntityPreferences = _haPreferences.Select(p => new PortableHaEntityPreference
        {
            EntityId = p.EntityId, Alias = p.Alias, IsFavorite = p.IsFavorite
        }).ToList(),
        BrightnessPercent = _preferredBrightness,
        ThemeMode = _preferredTheme,
        LanguageMode = _preferredLanguage,
        Fans = PortableFanSettings.Normalize(_fanSettings),
        ProgramCustomGlyphsOnConnect = this.FindControl<CheckBox>("ProgramGlyphsOnConnect")!.IsChecked == true
    };

    private async Task SavePortableWorkspaceAsync(bool throwOnFailure = false)
    {
        if (!_workspaceLoaded) return;
        try
        {
            await _workspaceStore.SaveAsync(CapturePortableWorkspace());
        }
        catch (Exception ex)
        {
            SetVirtualStatus($"Workspace save failed: {ex.Message}");
            if (throwOnFailure) throw;
        }
    }
}
