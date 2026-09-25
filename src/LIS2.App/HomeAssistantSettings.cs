using System.Text.Json.Serialization;

namespace LIS2.App;

public sealed class HomeAssistantSettings
{
    public bool Enabled { get; set; }
    public string Url { get; set; } = string.Empty;
    [JsonIgnore]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("AccessToken")]
    public string ProtectedAccessToken
    {
        get => HomeAssistantTokenProtector.Protect(AccessToken);
        set => AccessToken =
            HomeAssistantTokenProtector.UnprotectOrMigrateLegacy(value);
    }
    public List<HomeAssistantEntityPreferenceSettings> EntityPreferences { get; set; } = new();

    public void EnsureDefaults()
    {
        Url ??= string.Empty;
        AccessToken ??= string.Empty;
        EntityPreferences ??= new List<HomeAssistantEntityPreferenceSettings>();

        EntityPreferences = EntityPreferences
            .Where(item => !string.IsNullOrWhiteSpace(item.EntityId))
            .GroupBy(item => item.EntityId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToList();
    }
}

public sealed class HomeAssistantEntityPreferenceSettings
{
    public string EntityId { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
}
