using System.Text.Json;

namespace LIS2.App.Avalonia;

/// <summary>Reads the original Windows settings.json without modifying it or interpreting DPAPI secrets.</summary>
public static class WindowsSettingsImporter
{
    public static PortableWorkspace Import(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Windows settings must be a JSON object.");

        var result = new PortableWorkspace();
        if (root.TryGetProperty("Pages", out var pages))
        {
            if (pages.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Windows Pages must be an array.");
            var workspace = new PageWorkspace();
            workspace.ImportJson(pages.GetRawText());
            result.Pages = JsonSerializer.Deserialize<List<PageDefinition>>(workspace.ExportJson())!;
        }

        if (root.TryGetProperty("CustomGlyphs", out var glyphs))
        {
            if (glyphs.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Windows CustomGlyphs must be an array.");
            var parsed = JsonSerializer.Deserialize<List<CustomGlyphSettings>>(glyphs.GetRawText())
                ?? throw new InvalidOperationException("Invalid Windows custom glyphs.");
            if (parsed.Count != 8)
                throw new InvalidOperationException("Windows settings require exactly eight custom glyphs.");
            foreach (var glyph in parsed)
                glyph.EnsureDefaults();
            var glyphWorkspace = new GlyphWorkspace();
            glyphWorkspace.Replace(parsed);
            result.CustomGlyphs = glyphWorkspace.Glyphs.Select(g => new CustomGlyphSettings
            {
                Name = g.Name, Rows = g.Rows.ToArray()
            }).ToList();
        }

        if (root.TryGetProperty("HomeAssistant", out var ha) && ha.ValueKind == JsonValueKind.Object)
        {
            if (ha.TryGetProperty("Url", out var url) && url.ValueKind == JsonValueKind.String)
                result.HomeAssistantUrl = url.GetString() ?? string.Empty;
            if (ha.TryGetProperty("EntityPreferences", out var preferences) &&
                preferences.ValueKind == JsonValueKind.Array)
            {
                result.HomeAssistantEntityPreferences =
                    JsonSerializer.Deserialize<List<PortableHaEntityPreference>>(preferences.GetRawText())
                    ?? new();
            }
        }

        // Intentionally never read, decrypt, serialize, or copy HomeAssistant.AccessToken.
        // Other Windows-only settings remain in the untouched original file.
        return result;
    }
}

public sealed class PortableHaEntityPreference
{
    public string EntityId { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
}
