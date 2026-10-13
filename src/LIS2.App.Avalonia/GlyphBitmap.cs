namespace LIS2.App.Avalonia;

public static class GlyphBitmap
{
    public static byte[] Parse(string? text)
    {
        if (text is null)
            throw new ArgumentException("Glyph bitmap is required.", nameof(text));

        var rows = text.Replace("\r", "", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (rows.Length != 8)
            throw new ArgumentException("A glyph requires eight rows.", nameof(text));

        var result = new byte[8];
        for (var i = 0; i < rows.Length; i++)
        {
            if (rows[i].Length != 5 || rows[i].Any(c => c != '0' && c != '1'))
                throw new ArgumentException("Each glyph row requires five binary digits.", nameof(text));
            result[i] = Convert.ToByte(rows[i], 2);
        }
        return result;
    }

    public static string Format(byte[] rows)
    {
        if (rows.Length != 8 || rows.Any(v => v > 31))
            throw new ArgumentException("Expected eight five-bit rows.", nameof(rows));

        return string.Join("\n", rows.Select(v => Convert.ToString(v, 2).PadLeft(5, '0')));
    }
}
