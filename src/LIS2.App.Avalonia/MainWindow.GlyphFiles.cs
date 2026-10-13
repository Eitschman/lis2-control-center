using Avalonia.Platform.Storage;
using System.IO;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private async Task LoadGlyphSetAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Load glyph set" });
        if (files.Count == 0) return;
        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream);
        _glyphs.ImportJson(await reader.ReadToEndAsync());
        LoadGlyphEditor();
        await SavePortableWorkspaceAsync();
    }

    private async Task SaveGlyphSetAsync()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save glyph set",
            SuggestedFileName = "lis2-glyphs.lis2glyphs.json"
        });
        if (file is null) return;
        await using var stream = await file.OpenWriteAsync();
        stream.SetLength(0);
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(_glyphs.ExportJson());
        await writer.FlushAsync();
    }
}
