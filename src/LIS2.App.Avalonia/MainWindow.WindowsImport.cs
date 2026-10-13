using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.IO;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private void InitializeWindowsSettingsImport()
    {
        this.FindControl<Button>("ImportWindowsSettings")!.Click += async (_, _) =>
        {
            try
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Import original Windows LIS2 settings.json (read-only)",
                    AllowMultiple = false
                });
                if (files.Count == 0) return;
                await using var stream = await files[0].OpenReadAsync();
                using var reader = new StreamReader(stream);
                var before = CapturePortableWorkspace();
                var imported = WindowsSettingsImporter.Import(await reader.ReadToEndAsync(), before);
                // Validate, persist first, then apply. No UI changes if persistence fails.
                if (imported.Pages.Count > 0)
                {
                    var validation = new PageWorkspace();
                    validation.ImportJson(System.Text.Json.JsonSerializer.Serialize(imported.Pages));
                }
                if (imported.CustomGlyphs.Count == 8)
                {
                    var validation = new GlyphWorkspace();
                    validation.Replace(imported.CustomGlyphs);
                }
                await _workspaceStore.SaveAsync(imported);
                try
                {
                    ApplyPortableWorkspace(imported);
                }
                catch
                {
                    // Restore both disk and UI after an unexpected application failure.
                    try { await _workspaceStore.SaveAsync(before); }
                    finally { ApplyPortableWorkspace(before); }
                    throw;
                }
                RefreshPageVariables();
                this.FindControl<TextBlock>("WindowsImportStatus")!.Text =
                    $"Imported {imported.Pages.Count} pages, {imported.CustomGlyphs.Count} glyphs; token not imported.";
            }
            catch (Exception ex)
            {
                this.FindControl<TextBlock>("WindowsImportStatus")!.Text = $"Import failed: {ex.Message}";
            }
        };
    }
}
