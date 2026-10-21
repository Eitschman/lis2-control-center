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
            _ = SavePortableWorkspaceAsync();
        };
        this.FindControl<Button>("SendAllGlyphs")!.Click += async (_, _) =>
            await RunSelectedDisplayActionAsync(session => session.ProgramAllGlyphsAsync(_glyphs.Glyphs));
        this.FindControl<Button>("SaveGlyphSet")!.Click += async (_, _) =>
            await RunGlyphFileActionAsync(SaveGlyphSetAsync);
        this.FindControl<Button>("LoadGlyphSet")!.Click += async (_, _) =>
            await RunGlyphFileActionAsync(LoadGlyphSetAsync);
        LoadGlyphEditor();
    }

    private async Task RunGlyphFileActionAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { SetVirtualStatus($"Glyph file error: {(UiMessage)ex.Message}"); }
    }

    private void SaveGlyphEditor()
    {
        try
        {
            var slot = this.FindControl<ComboBox>("GlyphSlot")!.SelectedIndex + 1;
            _glyphs.SaveSlot(slot, this.FindControl<TextBox>("GlyphName")!.Text,
                this.FindControl<TextBox>("GlyphBitmapEditor")!.Text);
            RefreshGlyphPreview();
            SetVirtualStatus($"Saved glyph slot {slot} in workspace.");
            _ = SavePortableWorkspaceAsync();
        }
        catch (Exception ex)
        {
            SetVirtualStatus($"Glyph error: {(UiMessage)ex.Message}");
        }
    }

    private async Task ConnectWithGlyphsAsync()
    {
        await _virtualDevice.ConnectAsync();
        await ApplyConnectedDisplaySettingsAsync(_virtualDevice);
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
            SetUiText("GlyphPixelPreview", "Invalid 5x8 bitmap");
        }
    }
}
