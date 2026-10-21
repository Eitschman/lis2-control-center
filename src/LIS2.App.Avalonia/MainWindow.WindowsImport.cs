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
                    Title = UiText("Import original Windows LIS2 settings.json (read-only)"),
                    AllowMultiple = false
                });
                if (files.Count == 0) return;
                await using var stream = await files[0].OpenReadAsync();
                using var reader = new StreamReader(stream);
                await ImportWindowsSettingsAsync(await reader.ReadToEndAsync());
            }
            catch (Exception ex)
            {
                SetUiText("WindowsImportStatus", $"Import failed: {(UiMessage)ex.Message}");
            }
        };
    }

    /// <summary>Validiert und speichert den Import vor dem Anwenden; Editorentwürfe bleiben geschützt.</summary>
    internal async Task ImportWindowsSettingsAsync(string json)
    {
        if (_settingsTransportBusy || _dashboardFansBusy || _serialBusy || _busy)
            throw new InvalidOperationException("A device action is still running.");
        if (!CommitPendingPageEditor())
            throw new InvalidOperationException("Page draft contains invalid settings.");
        var before = CapturePortableWorkspace();
        var imported = WindowsSettingsImporter.Import(json, before);
        // Validate, persist first, then apply. No UI changes if persistence fails.
        PageWorkspaceActions.RestorePages(new PageWorkspace(), imported);
        if (imported.CustomGlyphs.Count == 8)
        {
            var validation = new GlyphWorkspace();
            validation.Replace(imported.CustomGlyphs);
        }
        _settingsTransportBusy = true;
        UpdateVirtualControls();
        UpdateHardwareFanControls();
        try
        {
            await _workspaceStore.SaveAsync(imported);
            try
            {
                await DisconnectTransportsAsync();
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
            await _pageSources.ConfigureHomeAssistantAsync(null, null);
            SetUiText("HaStatus", "Import applied. Reconnect Home Assistant if needed; token was not imported.");
        }
        finally
        {
            _settingsTransportBusy = false;
            UpdateVirtualControls();
            UpdateHardwareFanControls();
        }
        await ApplySettingsTransportAsync();
        SetUiText("WindowsImportStatus", $"Imported {imported.Pages.Count} pages, {imported.CustomGlyphs.Count} glyphs; token not imported.");
    }
}
