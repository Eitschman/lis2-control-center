using Avalonia.Controls;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly PortableWorkspaceStore _workspaceStore = new();
    private readonly PortableHaTokenStore _haTokenStore = new();
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
        try
        {
            var token = _haTokenStore.Load();
            if (!string.IsNullOrWhiteSpace(token))
            {
                this.FindControl<TextBox>("HaToken")!.Text = token;
                this.FindControl<CheckBox>("HaEnabled")!.IsChecked = true;
            }
        }
        catch (Exception ex)
        {
            SetVirtualStatus($"Home Assistant credential load failed: {ex.Message}");
        }
        if (saved.CustomGlyphs.Count == 8)
        {
            _glyphs.Replace(saved.CustomGlyphs);
            LoadGlyphEditor();
        }
        _haPreferences = saved.HomeAssistantEntityPreferences.ToList();
        _favoritePageVariables.Clear();
        foreach (var key in saved.PageVariableFavorites.Where(key => !string.IsNullOrWhiteSpace(key)))
            _favoritePageVariables.Add(key);
        _hardwarePreferences.Clear();
        _hardwarePreferences.AddRange(saved.HardwareSensorPreferences.Where(p => !string.IsNullOrWhiteSpace(p.Key)));
        FilterHardwareSensors();
        _preferredBrightness = saved.BrightnessPercent;
        _preferredTheme = saved.ThemeMode;
        _preferredLanguage = saved.LanguageMode;
        LoadSettingsPreferences();
        _fanSettings = PortableFanSettings.Normalize(saved.Fans);
        RefreshFanEditor();
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
        HardwareSensorPreferences = _hardwarePreferences.Select(p => new PortableHardwareSensorPreference
        {
            Key = p.Key, Alias = p.Alias, IsFavorite = p.IsFavorite
        }).ToList(),
        HomeAssistantEntityPreferences = _haPreferences.Select(p => new PortableHaEntityPreference
        {
            EntityId = p.EntityId, Alias = p.Alias, IsFavorite = p.IsFavorite
        }).ToList(),
        PageVariableFavorites = _favoritePageVariables.OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToList(),
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
