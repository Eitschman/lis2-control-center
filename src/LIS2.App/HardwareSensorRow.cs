namespace LIS2.App;

public sealed record HardwareSensorRow(
    string Key,
    string Name,
    string TypeKey,
    string Type,
    string Value,
    string Unit,
    string Alias,
    bool IsFavorite)
{
    public string DisplayName =>
        string.IsNullOrWhiteSpace(Alias)
            ? Name
            : Alias;

    public string FavoriteMarker => IsFavorite ? "★" : string.Empty;

    public string DisplayValue =>
        string.IsNullOrWhiteSpace(Unit)
            ? Value
            : $"{Value} {Unit}";
}
