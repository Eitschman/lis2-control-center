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
                var imported = WindowsSettingsImporter.Import(await reader.ReadToEndAsync());
                // Apply only after complete parsing and validation; never overwrite the Windows file.
                ApplyPortableWorkspace(imported);
                await SavePortableWorkspaceAsync();
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
