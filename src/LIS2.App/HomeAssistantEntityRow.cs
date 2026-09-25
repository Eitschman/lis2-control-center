namespace LIS2.App;

public sealed record HomeAssistantEntityRow(
    string EntityId,
    string Domain,
    string FriendlyName,
    string State,
    string Unit,
    string Alias,
    bool IsFavorite,
    bool IsAvailable)
{
    public string DisplayName =>
        string.IsNullOrWhiteSpace(Alias)
            ? FriendlyName
            : Alias;

    public string FavoriteMarker => IsFavorite ? "★" : string.Empty;

    public string DisplayState =>
        string.IsNullOrWhiteSpace(Unit)
            ? State
            : $"{State} {Unit}";

    public string TemplateKey =>
        $"{{HA.{EntityId}}}";

    public string? AliasTemplateKey =>
        string.IsNullOrWhiteSpace(Alias)
            ? null
            : $"{{HA.{NormalizeAlias(Alias)}}}";

    private static string NormalizeAlias(string alias)
    {
        var chars = alias
            .Trim()
            .Select(character =>
                char.IsLetterOrDigit(character) || character is '_' or '-'
                    ? character
                    : '_')
            .ToArray();

        return new string(chars)
            .Trim('_')
            .Replace("__", "_", StringComparison.Ordinal);
    }
}
