namespace LIS2.App;

public sealed record HardwareSensorRow(
    string Key,
    string Name,
    string TypeKey,
    string Type,
    string Value,
    string Unit)
{
    public string DisplayValue =>
        string.IsNullOrWhiteSpace(Unit)
            ? Value
            : $"{Value} {Unit}";
}
