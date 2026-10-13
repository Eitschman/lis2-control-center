using Avalonia.Controls;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly GlyphWorkspace _glyphs = new();

    private void InitializeGlyphEditor()
    {
        this.FindControl<ComboBox>("GlyphSlot")!.SelectionChanged += (_, _) => LoadGlyphEditor();
        this.FindControl<TextBox>("GlyphBitmapEditor")!.TextChanged += (_, _) => RefreshGlyphPreview();
        this.FindControl<TextBox>("GlyphName")!.TextChanged += (_, _) => RefreshGlyphPreview();
        this.FindControl<Button>("SaveGlyph")!.Click += (_, _) => SaveGlyphEditor();
        this.FindControl<Button>("LoadExampleGlyphs")!.Click += (_, _) =>
        {
            _glyphs.LoadExamples();
            LoadGlyphEditor();
        };
        LoadGlyphEditor();
    }
}
