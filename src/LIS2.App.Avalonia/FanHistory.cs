namespace LIS2.App.Avalonia;

internal sealed record FanHistorySample(DateTimeOffset Time, double? Sensor, double? Target, double? Rpm);

/// <summary>Begrenzter Sitzungsverlauf. Fehlende Werte bleiben Lücken; Sensorwechsel trennen Reihen.</summary>
internal sealed class FanHistory
{
    internal static readonly TimeSpan Retention = TimeSpan.FromHours(1);
    internal const int Capacity = 1801;
    private readonly Queue<FanHistorySample>[] _samples = Enumerable.Range(0, 4).Select(_ => new Queue<FanHistorySample>()).ToArray();
    private readonly string?[] _identities = new string?[4];

    internal void Add(int channel, string identity, FanHistorySample sample)
    {
        var samples = _samples[channel];
        if (_identities[channel] != identity)
        {
            // Verschiedene Sensoren und Regelkonfigurationen dürfen keine durchgehende Kurve bilden.
            samples.Clear();
            _identities[channel] = identity;
        }
        if (samples.Count > 0 && sample.Time <= samples.Last().Time) return;
        samples.Enqueue(sample with { Sensor = Finite(sample.Sensor), Target = Finite(sample.Target), Rpm = sample.Rpm >= 0 ? Finite(sample.Rpm) : null });
        while (samples.Count > Capacity || samples.Count > 0 && sample.Time - samples.Peek().Time > Retention) samples.Dequeue();
    }

    internal FanHistorySample[] Read(int channel, DateTimeOffset now, TimeSpan window) =>
        _samples[channel].Where(sample => sample.Time >= now - window && sample.Time <= now).ToArray();

    private static double? Finite(double? value) => value.HasValue && double.IsFinite(value.Value) ? value : null;
}
