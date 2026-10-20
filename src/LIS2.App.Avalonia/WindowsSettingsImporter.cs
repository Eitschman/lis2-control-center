using System.Text.Json;

namespace LIS2.App.Avalonia;

/// <summary>Reads the original Windows settings.json without modifying it or interpreting DPAPI secrets.</summary>
public static class WindowsSettingsImporter
{
    public static PortableWorkspace Import(string json, PortableWorkspace? existing = null)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Windows settings must be a JSON object.");

        var result = existing is null ? new PortableWorkspace() : JsonSerializer.Deserialize<PortableWorkspace>(JsonSerializer.Serialize(existing))!;
        if (root.TryGetProperty("TransportMode", out var transport))
        {
            if (transport.ValueKind != JsonValueKind.String || transport.GetString() is not ("Serial" or "Virtual"))
                throw new InvalidOperationException("Invalid Windows transport mode.");
            result.TransportMode = transport.GetString()!;
        }
        if (root.TryGetProperty("PortName", out var port))
        {
            if (port.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                throw new InvalidOperationException("Invalid Windows port name.");
            result.PortName = port.GetString();
        }
        if (root.TryGetProperty("BrightnessPercent", out var brightness) &&
            brightness.ValueKind == JsonValueKind.Number &&
            brightness.TryGetInt32(out var percent) &&
            percent is 25 or 50 or 75 or 100)
            result.BrightnessPercent = percent;
        if (root.TryGetProperty("ProgramCustomGlyphsOnConnect", out var programGlyphs) &&
            programGlyphs.ValueKind is JsonValueKind.True or JsonValueKind.False)
            result.ProgramCustomGlyphsOnConnect = programGlyphs.GetBoolean();
        if (root.TryGetProperty("ThemeMode", out var theme) && theme.ValueKind == JsonValueKind.String)
        {
            var mode = theme.GetString();
            if (mode is "System" or "Light" or "Dark")
                result.ThemeMode = mode;
        }
        if (root.TryGetProperty("LanguageMode", out var language) && language.ValueKind == JsonValueKind.String)
            result.LanguageMode = language.GetString() ?? "System";

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
            if (ha.TryGetProperty("Enabled", out var enabled))
            {
                if (enabled.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    throw new InvalidOperationException("Invalid Home Assistant enabled setting.");
                result.HomeAssistantEnabled = enabled.GetBoolean();
            }
            if (ha.TryGetProperty("Url", out var url) && url.ValueKind == JsonValueKind.String)
                result.HomeAssistantUrl = url.GetString() ?? string.Empty;
            if (ha.TryGetProperty("EntityPreferences", out var preferences) &&
                preferences.ValueKind == JsonValueKind.Array)
            {
                result.HomeAssistantEntityPreferences =
                    JsonSerializer.Deserialize<List<PortableHaEntityPreference>>(preferences.GetRawText())
                    ?? new();
                result.HomeAssistantEntityPreferences = result.HomeAssistantEntityPreferences
                    .Where(p => p is not null && !string.IsNullOrWhiteSpace(p.EntityId))
                    .GroupBy(p => p.EntityId, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.Last()).ToList();
            }
        }

        if (root.TryGetProperty("HardwareSensorPreferences", out var hardware))
        {
            if (hardware.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Windows hardware preferences must be an array.");
            result.HardwareSensorPreferences =
                (JsonSerializer.Deserialize<List<PortableHardwareSensorPreference>>(hardware.GetRawText()) ?? new())
                .Where(preference => preference is not null && !string.IsNullOrWhiteSpace(preference.Key))
                .GroupBy(preference => preference.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.Last()).ToList();
        }

        if (root.TryGetProperty("Fans", out var fans))
        {
            if (fans.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Windows Fans must be an object.");
            result.Fans = PortableFanSettings.Normalize(JsonSerializer.Deserialize<PortableFanSettings>(fans.GetRawText()));
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
