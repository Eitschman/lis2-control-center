using System.Text.Json;

namespace LIS2.App.Avalonia;

/// <summary>Eight-slot glyph editor state using the original Windows glyph-set JSON format.</summary>
public sealed class GlyphWorkspace
{
    private List<CustomGlyphSettings> _glyphs = NewEmpty();
    public IReadOnlyList<CustomGlyphSettings> Glyphs => _glyphs;

    public void SaveSlot(int slot, string? name, string? bitmap)
    {
        ValidateSlot(slot);
        _glyphs[slot - 1] = new CustomGlyphSettings
        {
            Name = string.IsNullOrWhiteSpace(name) ? $"Glyph {slot}" : name.Trim(),
            Rows = GlyphBitmap.Parse(bitmap)
        };
    }

    public CustomGlyphSettings GetSlot(int slot)
    {
        ValidateSlot(slot);
        var glyph = _glyphs[slot - 1];
        return new CustomGlyphSettings { Name = glyph.Name, Rows = glyph.Rows.ToArray() };
    }

    public void LoadExamples() => Replace(BuiltInGlyphSets.CreateExampleSet());

    public bool IsSpectrumSet => BuiltInGlyphSets.IsSpectrumSet(_glyphs);

    public void LoadSpectrumSet() => Replace(BuiltInGlyphSets.CreateSpectrumSet());

    public void Replace(IEnumerable<CustomGlyphSettings> glyphs)
    {
        var copy = glyphs.Select(g => new CustomGlyphSettings
        {
            Name = g.Name,
            Rows = g.Rows?.ToArray() ?? Array.Empty<byte>()
        }).ToList();
        if (copy.Count != 8 || copy.Any(g => g.Rows.Length != 8 || g.Rows.Any(v => v > 31)))
            throw new InvalidOperationException("A glyph set requires eight valid 5x8 glyphs.");
        _glyphs = copy;
    }

    public string ExportJson(string name = "Custom glyph set") =>
        JsonSerializer.Serialize(new CustomGlyphSetFile
        {
            Name = name,
            Glyphs = _glyphs.Select(g => new CustomGlyphSettings
            {
                Name = g.Name,
                Rows = g.Rows.ToArray()
            }).ToList()
        }, new JsonSerializerOptions { WriteIndented = true });

    public void ImportJson(string json)
    {
        var set = JsonSerializer.Deserialize<CustomGlyphSetFile>(json)
            ?? throw new InvalidOperationException("The glyph-set file is empty.");
        if (set.Version != 1)
            throw new InvalidOperationException($"Unsupported glyph-set version {set.Version}.");
        Replace(set.Glyphs);
    }

    private static List<CustomGlyphSettings> NewEmpty() =>
        Enumerable.Range(1, 8).Select(slot => new CustomGlyphSettings
        {
            Name = $"Glyph {slot}",
            Rows = new byte[8]
        }).ToList();

    private static void ValidateSlot(int slot)
    {
        if (slot is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(slot));
    }
}
