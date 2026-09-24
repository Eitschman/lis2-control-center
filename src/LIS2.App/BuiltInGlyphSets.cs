namespace LIS2.App;

public static class BuiltInGlyphSets
{
    public static IReadOnlyList<CustomGlyphSettings> CreateExampleSet() =>
        new[]
        {
            Glyph("Play",        "10000","11000","11100","11110","11100","11000","10000","00000"),
            Glyph("Pause",       "11011","11011","11011","11011","11011","11011","11011","00000"),
            Glyph("Thermometer", "00100","01010","01010","01010","01110","11111","11111","01110"),
            Glyph("Fan",         "00100","10101","01110","11111","01110","10101","00100","00000"),
            Glyph("Up",          "00100","01110","10101","00100","00100","00100","00100","00000"),
            Glyph("Down",        "00100","00100","00100","00100","10101","01110","00100","00000"),
            Glyph("BarEmpty",    "11111","10001","10001","10001","10001","10001","11111","00000"),
            Glyph("BarFull",     "11111","11111","11111","11111","11111","11111","11111","00000")
        };

    public static IReadOnlyList<CustomGlyphSettings> CreateSpectrumSet() =>
        Enumerable.Range(1, 8)
            .Select(level => Glyph(
                $"Spectrum{level}",
                Enumerable.Range(0, 8)
                    .Select(row => row >= 8 - level ? "11111" : "00000")
                    .ToArray()))
            .ToArray();

    public static bool IsSpectrumSet(IReadOnlyList<CustomGlyphSettings> glyphs)
    {
        if (glyphs.Count != 8)
            return false;

        var expected = CreateSpectrumSet();

        for (var index = 0; index < 8; index++)
        {
            if (!glyphs[index].Rows.SequenceEqual(expected[index].Rows))
                return false;
        }

        return true;
    }

    private static CustomGlyphSettings Glyph(string name, params string[] rows) =>
        new()
        {
            Name = name,
            Rows = rows.Select(row => Convert.ToByte(row, 2)).ToArray()
        };
}
