namespace LIS2.Sources;

public sealed class SensorRegistry
{
    private readonly Dictionary<string, SensorValue> _values =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<SensorValue> Values => _values.Values;

    public void Update(SensorValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _values[value.Id] = value;
    }

    public SensorValue? Get(string id) =>
        _values.TryGetValue(id, out var value) ? value : null;
}
