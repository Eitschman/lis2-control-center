namespace LIS2.App;

public sealed record HardwareSensorRow(
    string Key,
    string HardwareName,
    string SensorName,
    string TypeKey,
    string Type,
    string Value,
    string Unit,
    string Alias,
    bool IsFavorite,
    bool IsAvailable)
{
    public string Name => $"{HardwareName} — {SensorName}";

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Alias)
            ? SensorName
            : Alias;

    public string FavoriteMarker => IsFavorite ? "★" : string.Empty;

    public string AvailabilityMarker => IsAvailable ? string.Empty : "⚠";

    public string StatusText =>
        IsAvailable
            ? string.Empty
            : LocalizationService.Translate("Unavailable");

    public string DisplayValue =>
        !IsAvailable
            ? LocalizationService.Translate("Unavailable")
            : string.IsNullOrWhiteSpace(Unit)
                ? Value
                : $"{Value} {Unit}";
}
