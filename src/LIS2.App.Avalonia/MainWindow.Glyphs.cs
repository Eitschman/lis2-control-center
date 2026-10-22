using Avalonia.Controls;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly GlyphWorkspace _glyphs = new();
    private readonly global::Avalonia.Controls.Primitives.ToggleButton[] _glyphPixels = new global::Avalonia.Controls.Primitives.ToggleButton[40];

    private void InitializeGlyphEditor()
    {
        var grid = this.FindControl<global::Avalonia.Controls.Primitives.UniformGrid>("GlyphPixelGrid")!;
        for (var index = 0; index < _glyphPixels.Length; index++)
        {
            var pixel = index;
            var button = new global::Avalonia.Controls.Primitives.ToggleButton
            {
                Width = 24, Height = 24, MinWidth = 0, Padding = new global::Avalonia.Thickness(0),
                BorderThickness = new global::Avalonia.Thickness(1), BorderBrush = global::Avalonia.Media.Brush.Parse("#466452"),
                Margin = new global::Avalonia.Thickness(1)
            };
            global::Avalonia.Automation.AutomationProperties.SetName(button, $"Pixel {index / 5 + 1}, {index % 5 + 1}");
            button.Click += (_, _) =>
            {
                try
                {
                    var rows = GlyphBitmap.Parse(this.FindControl<TextBox>("GlyphBitmapEditor")!.Text);
                    rows[pixel / 5] ^= (byte)(1 << (4 - pixel % 5));
                    this.FindControl<TextBox>("GlyphBitmapEditor")!.Text = GlyphBitmap.Format(rows);
                }
                catch (ArgumentException exception) { SetUiText("GlyphSaveStatus", $"Glyph error: {(UiMessage)exception.Message}"); }
            };
            grid.Children.Add(button);
            _glyphPixels[index] = button;
        }
        this.FindControl<ComboBox>("GlyphSlot")!.SelectionChanged += (_, _) => LoadGlyphEditor();
        foreach (var name in new[] { "GlyphBitmapEditor", "GlyphName" })
            this.FindControl<TextBox>(name)!.PropertyChanged += (_, change) =>
            {
                if (change.Property != TextBox.TextProperty) return;
                RefreshGlyphPreview();
                MarkEditorChanged("GlyphSaveStatus");
            };
        this.FindControl<Button>("SaveGlyph")!.Click += async (_, _) => await SaveGlyphEditorAsync();
        this.FindControl<Button>("LoadExampleGlyphs")!.Click += async (_, _) =>
        {
            _glyphs.LoadExamples();
            LoadGlyphEditor();
            await SaveAndSynchronizeGlyphSetAsync();
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

    private async Task SaveGlyphEditorAsync()
    {
        try
        {
            var slot = this.FindControl<ComboBox>("GlyphSlot")!.SelectedIndex + 1;
            _glyphs.SaveSlot(slot, this.FindControl<TextBox>("GlyphName")!.Text,
                this.FindControl<TextBox>("GlyphBitmapEditor")!.Text);
            _runtime.Sources.SetGlyphNames(_glyphs.Glyphs.Select(glyph => glyph.Name), _glyphs.IsSpectrumSet);
            RefreshGlyphPreview();
            await PersistEditorAsync("GlyphSaveStatus");
        }
        catch (Exception ex)
        {
            SetUiText("GlyphSaveStatus", $"Glyph error: {(UiMessage)ex.Message}");
        }
    }

    /// <summary>Nur die gespeicherte Glyphenoption erlaubt die Synchronisierung eines geladenen Satzes.</summary>
    private async Task SaveAndSynchronizeGlyphSetAsync()
    {
        if (!await PersistEditorAsync("GlyphSaveStatus")) return;
        if (this.FindControl<CheckBox>("ProgramGlyphsOnConnect")!.IsChecked == true && SelectedDisplay?.IsConnected == true)
        {
            var sent = false;
            await RunSelectedDisplayActionAsync(async session =>
            {
                await session.ProgramAllGlyphsAsync(_glyphs.Glyphs);
                sent = true;
            });
            SetUiText("GlyphSaveStatus", sent ? "Saved and sent to display." : "Saved; sending to display failed or is busy.");
        }
        RefreshPagePreview();
        UpdatePreview();
    }

    private async Task ConnectWithGlyphsAsync()
    {
        await _virtualDevice.ConnectAsync();
        await ApplyConnectedDisplaySettingsAsync(_virtualDevice);
    }

    private void LoadGlyphEditor()
    {
        _runtime.Sources.SetGlyphNames(_glyphs.Glyphs.Select(glyph => glyph.Name), _glyphs.IsSpectrumSet);
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
        var name = PageSourceValues.NormalizeGlyphName(this.FindControl<TextBox>("GlyphName")!.Text);
        this.FindControl<TextBlock>("GlyphTemplateKey")!.Text = name.Length == 0 || int.TryParse(name, out _)
            ? $"{{Glyph.{slot}}}" : $"{{Glyph.{slot}}}  {{Glyph.{name}}}";
        try
        {
            var rows = GlyphBitmap.Parse(this.FindControl<TextBox>("GlyphBitmapEditor")!.Text);
            SetUiText("GlyphPixelStatus", "");
            for (var index = 0; index < _glyphPixels.Length; index++)
                if (_glyphPixels[index] is { } pixel)
                {
                    pixel.IsEnabled = true;
                    pixel.IsChecked = (rows[index / 5] & (1 << (4 - index % 5))) != 0;
                }
        }
        catch (ArgumentException)
        {
            SetUiText("GlyphPixelStatus", "Invalid 5x8 bitmap");
            foreach (var pixel in _glyphPixels) if (pixel is not null) pixel.IsEnabled = false;
        }
    }
}
