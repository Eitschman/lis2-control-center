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

    private void LoadGlyphEditor()
    {
        var slot = this.FindControl<ComboBox>("GlyphSlot")!.SelectedIndex + 1;
        if (slot is < 1 or > 8) return;
        var glyph = _glyphs.GetSlot(slot);
        this.FindControl<TextBox>("GlyphName")!.Text = glyph.Name;
        this.FindControl<TextBox>("GlyphBitmapEditor")!.Text = GlyphBitmap.Format(glyph.Rows);
        RefreshGlyphPreview();
    }

    private void RefreshGlyphPreview()
    {
        var slot = this.FindControl<ComboBox>("GlyphSlot")!.SelectedIndex + 1;
        if (slot is < 1 or > 8) return;
        this.FindControl<TextBlock>("GlyphTemplateKey")!.Text = $"{{Glyph.{slot}}}";
        try
        {
            var rows = GlyphBitmap.Parse(this.FindControl<TextBox>("GlyphBitmapEditor")!.Text);
            this.FindControl<TextBlock>("GlyphPixelPreview")!.Text = GlyphBitmap.Preview(rows);
        }
        catch (ArgumentException)
        {
            this.FindControl<TextBlock>("GlyphPixelPreview")!.Text = "Invalid 5x8 bitmap";
        }
    }
}
