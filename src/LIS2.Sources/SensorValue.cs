namespace LIS2.Sources;

public sealed record SensorValue(
    string Id,
    string Name,
    double? Value,
    string Unit,
    DateTimeOffset Timestamp,
    bool IsValid);
