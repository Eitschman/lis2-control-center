using Avalonia.Controls;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly PortableWorkspaceStore _workspaceStore;
    private readonly PortableHaTokenStore _haTokenStore;
    private bool _workspaceLoaded;
    private List<PortableHaEntityPreference> _haPreferences = new();
    private int _preferredBrightness = 100;
    private string _preferredTheme = "System";
    private string _preferredLanguage = "System";
    private string _preferredTransportMode = "Virtual";
    private string? _preferredPortName;
    private PortableFanSettings _fanSettings = new();

    private void LoadPortableWorkspace()
    {
        try
        {
            var restorePages = _workspaceStore.Exists;
            var saved = _workspaceStore.Load();
            ApplyPortableWorkspace(saved, restorePages);
            _workspaceLoaded = true;
        }
        catch (Exception ex)
        {
            SetVirtualStatus($"Workspace load failed: {(UiMessage)ex.Message}");
            _workspaceLoaded = true;
        }
    }

    private void ApplyPortableWorkspace(PortableWorkspace saved, bool restorePages = true)
    {
        if (saved.TransportMode is not ("Serial" or "Virtual"))
            throw new InvalidOperationException("Invalid transport mode.");
        _ = BrightnessFromPercent(saved.BrightnessPercent);
        // Validate page data before mutating any live UI or workspace state.
        var validation = new PageWorkspace();
        if (restorePages) PageWorkspaceActions.RestorePages(validation, saved);
        this.FindControl<TextBox>("HaUrl")!.Text = saved.HomeAssistantUrl;
        this.FindControl<CheckBox>("HaEnabled")!.IsChecked = saved.HomeAssistantEnabled == true;
        try
        {
            var token = _haTokenStore.Load();
            if (!string.IsNullOrWhiteSpace(token))
            {
                this.FindControl<TextBox>("HaToken")!.Text = token;
                // Bestehende Workspaces ohne Aktivierungsfeld behalten ihr tokenbasiertes Verhalten.
                if (saved.HomeAssistantEnabled is null)
                    this.FindControl<CheckBox>("HaEnabled")!.IsChecked = true;
            }
        }
        catch (Exception ex)
        {
            SetVirtualStatus($"Home Assistant credential load failed: {(UiMessage)ex.Message}");
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
        _preferredTransportMode = saved.TransportMode;
        _preferredPortName = saved.PortName;
        _preferredTheme = saved.ThemeMode;
        _preferredLanguage = saved.LanguageMode;
        _selectedMediaPlayer = saved.LinuxMediaPlayer;
        UpdateMediaPlayerPicker([]);
        LoadSettingsPreferences();
        FilterHardwareSensors();
        _fanSettings = PortableFanSettings.Normalize(saved.Fans);
        RefreshFanEditor();
        this.FindControl<CheckBox>("ProgramGlyphsOnConnect")!.IsChecked = saved.ProgramCustomGlyphsOnConnect;
        // Ohne Datei bleibt die Startseite erhalten; eine gespeicherte leere Liste ersetzt sie.
        if (restorePages) PageWorkspaceActions.RestorePages(_pages, saved);
        RefreshPageList(_pages.Pages.Count > 0 ? 0 : -1);
    }

    private PortableWorkspace CapturePortableWorkspace() => new()
    {
        TransportMode = _preferredTransportMode,
        PortName = _preferredPortName,
        HomeAssistantEnabled = this.FindControl<CheckBox>("HaEnabled")!.IsChecked == true,
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
        LinuxMediaPlayer = _selectedMediaPlayer,
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
            SetVirtualStatus($"Workspace save failed: {(UiMessage)ex.Message}");
            if (throwOnFailure) throw;
        }
    }
}
