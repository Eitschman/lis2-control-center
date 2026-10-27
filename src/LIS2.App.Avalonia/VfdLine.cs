using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using LIS2.Display;

namespace LIS2.App.Avalonia;

/// <summary>Zeichnet eine VFD-Zeile in zwanzig festen Zellen, unabhängig von Unicode-Ersatzschriften.</summary>
public sealed class VfdLine : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        TextBlock.TextProperty.AddOwner<VfdLine>();

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    static VfdLine()
    {
        AffectsMeasure<VfdLine>(FontFamilyProperty, FontSizeProperty, FontStyleProperty,
            FontWeightProperty, FontStretchProperty);
        AffectsRender<VfdLine>(TextProperty, ForegroundProperty, FontFamilyProperty,
            FontSizeProperty, FontStyleProperty, FontWeightProperty, FontStretchProperty);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new VfdLineAutomationPeer(this);

    private sealed class VfdLineAutomationPeer : ControlAutomationPeer
    {
        public VfdLineAutomationPeer(VfdLine owner) : base(owner)
        {
            owner.PropertyChanged += (_, change) =>
            {
                if (change.Property == TextProperty)
                    RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty,
                        change.OldValue, change.NewValue);
            };
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
        protected override string? GetNameCore() => ((VfdLine)Owner).Text;
    }

    private TextLayout Layout(string text) => new(text,
        new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, Foreground);

    protected override Size MeasureOverride(Size availableSize)
    {
        using var reference = Layout(new string('0', DisplayFrame.Width));
        return new Size(reference.Width, reference.Height);
    }

    public override void Render(DrawingContext context)
    {
        using var reference = Layout(new string('0', DisplayFrame.Width));
        var cellWidth = reference.Width / DisplayFrame.Width;
        var cellHeight = reference.Height;
        var text = Text ?? string.Empty;
        for (var column = 0; column < Math.Min(text.Length, DisplayFrame.Width); column++)
        {
            var character = text[column];
            if (character == ' ') continue;
            var cell = new Rect(column * cellWidth, 0, cellWidth, cellHeight);
            using var clip = context.PushClip(cell);
            var level = "▁▂▃▄▅▆▇█".IndexOf(character) + 1;
            if (level > 0)
            {
                // Spectrum-Balken bilden die acht VFD-Stufen direkt ab und benötigen keine Ersatzschrift.
                var barHeight = cellHeight * level / 8;
                context.DrawRectangle(Foreground, null,
                    new Rect(cell.X, cellHeight - barHeight, Math.Max(0, cellWidth - 1), barHeight));
                continue;
            }
            using var glyph = Layout(character.ToString());
            var scaleX = glyph.Width > 0 ? Math.Min(1, cellWidth / glyph.Width) : 1;
            var scaleY = glyph.Height > 0 ? Math.Min(1, cellHeight / glyph.Height) : 1;
            var origin = new Point(cell.X + (cellWidth - glyph.Width * scaleX) / 2,
                (cellHeight - glyph.Height * scaleY) / 2);
            using var position = context.PushTransform(Matrix.CreateTranslation(origin.X, origin.Y));
            using var scale = context.PushTransform(Matrix.CreateScale(scaleX, scaleY));
            glyph.Draw(context, default);
        }
    }
}
