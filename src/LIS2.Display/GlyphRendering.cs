using LIS2.Core;

namespace LIS2.Display;

/// <summary>Bildschirmdarstellung und VFD-Balken verwenden dieselben acht Glyphenstufen.</summary>
public static class GlyphRendering
{
    public static string Preview(string text, bool spectrumSet) => new(text.Select(character =>
        Lis2Protocol.TryGetCustomGlyphSlot(character, out var slot)
            ? (spectrumSet ? "▁▂▃▄▅▆▇█" : "①②③④⑤⑥⑦⑧")[slot - 1]
            : character).ToArray());

    /// <summary>Winamp liefert je nach Telemetriepfad Pegel von 0..15 oder 0..255.</summary>
    public static string Spectrum(IReadOnlyList<int> spectrum, int? maximumLevel = null)
    {
        var width = Math.Min(DisplayFrame.Width, spectrum.Count);
        var peak = spectrum.Take(width).Select(value => Math.Max(0, value)).DefaultIfEmpty().Max();
        var maximum = maximumLevel ?? (peak <= 15 ? 15d : 255d);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximum);
        return new string(Enumerable.Range(0, DisplayFrame.Width).Select(index =>
        {
            var value = index < width ? Math.Clamp(spectrum[index], 0, (int)maximum) : 0;
            return value == 0 ? ' ' : Lis2Protocol.CustomGlyph(Math.Clamp((int)Math.Ceiling(value / maximum * 8), 1, 8));
        }).ToArray());
    }
}
