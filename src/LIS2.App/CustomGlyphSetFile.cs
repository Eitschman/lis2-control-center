namespace LIS2.App;

public sealed class CustomGlyphSetFile
{
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "Custom glyph set";
    public List<CustomGlyphSettings> Glyphs { get; set; } = new();

    public void Validate()
    {
        if (Version != 1)
            throw new InvalidOperationException($"Unsupported glyph-set version {Version}.");

        if (Glyphs is null || Glyphs.Count != 8)
            throw new InvalidOperationException("A glyph set must contain exactly eight glyphs.");

        foreach (var glyph in Glyphs)
            glyph.EnsureDefaults();
    }
}
