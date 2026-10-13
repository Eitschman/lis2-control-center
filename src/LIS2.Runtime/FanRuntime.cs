using System.Globalization;
using LIS2.Fans;

namespace LIS2.Runtime;

/// <summary>A read-only sensor snapshot provider; missing/nonfinite values trigger fan fail-safe.</summary>
public interface IFanSensorSource
{
    IReadOnlyDictionary<string, object?> Snapshot();
}

/// <summary>Hardware-neutral fan output. Phase 1 must never instantiate a physical transport.</summary>
public interface IFanOutputSink
{
    Task WriteAsync(IReadOnlyList<int> outputs, CancellationToken cancellationToken);
}

public sealed record FanChannelState(int OutputPercent, double? SensorValue, bool SensorValid);
public sealed record FanRuntimeState(IReadOnlyList<FanChannelState> Channels, bool Applied);

/// <summary>
/// Four-channel controller matching WPF ApplyAutomaticFanControlAsync semantics:
/// hysteresis uses the last successfully applied output and last observed sensor.
/// Serialized ticks and writes; no speculative reconnect, hardware or startup writes.
/// </summary>
public sealed class FanRuntime : IAsyncDisposable
{
    private readonly FanController _controller = new();
    private readonly IFanSensorSource _sensors;
    private readonly IFanOutputSink _output;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private FanChannelConfiguration[] _channels;
    private double?[] _previousSensors = new double?[4];
    private int[]? _lastApplied;
    private bool _disposed;

    public FanRuntime(IFanSensorSource sensors, IFanOutputSink output,
        IReadOnlyList<FanChannelConfiguration> channels)
    {
        _sensors = sensors ?? throw new ArgumentNullException(nameof(sensors));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _channels = CopyAndValidate(channels);
    }

    public bool IsOutputEnabled { get; private set; }
    public IReadOnlyList<int>? LastApplied => _lastApplied?.ToArray();

    /// <summary>Explicit opt-in only. Never enabled by construction or by loading configuration.</summary>
    public async Task SetOutputEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            IsOutputEnabled = enabled;
            if (!enabled) ResetHistory();
        }
        finally { _gate.Release(); }
    }

    public async Task ConfigureAsync(IReadOnlyList<FanChannelConfiguration> channels,
        CancellationToken cancellationToken = default)
    {
        var copy = CopyAndValidate(channels);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            _channels = copy;
            ResetHistory();
        }
        finally { _gate.Release(); }
    }

    public async Task<FanRuntimeState> TickAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            var snapshot = _sensors.Snapshot();
            var outputs = new int[4];
            var sensorValues = new double?[4];
            var states = new FanChannelState[4];
            for (var i = 0; i < 4; i++)
            {
                var config = _channels[i];
                var requiresSensor = config.Mode is FanMode.Curve or FanMode.Follow;
                var valid = !requiresSensor ||
                    (!string.IsNullOrWhiteSpace(config.SensorKey) &&
                     snapshot.TryGetValue(config.SensorKey, out var raw) &&
                     TryConvertSensor(raw, out _));
                double? sensor = null;
                if (valid && requiresSensor)
                {
                    snapshot.TryGetValue(config.SensorKey!, out var sensorRaw);
                    TryConvertSensor(sensorRaw, out sensor);
                }
                sensorValues[i] = valid ? sensor : null;
                outputs[i] = _controller.CalculateOutput(config, sensor, null, valid,
                    _previousSensors[i], _lastApplied?[i]);
                states[i] = new FanChannelState(outputs[i], sensorValues[i], valid);
            }

            // A dry-run does not mutate hysteresis history or pretend hardware was updated.
            if (!IsOutputEnabled) return new FanRuntimeState(states, false);

            var changed = _lastApplied is null || !outputs.SequenceEqual(_lastApplied);
            if (changed)
            {
                // Preserve previous state on a failed write; retry on the next tick.
                await _output.WriteAsync(outputs, cancellationToken).ConfigureAwait(false);
                _lastApplied = outputs;
            }
            _previousSensors = sensorValues;
            return new FanRuntimeState(states, changed);
        }
        finally { _gate.Release(); }
    }

    /// <summary>Disconnect/reset prevents stale outputs and sensor history from surviving reconnection.</summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await SetOutputEnabledAsync(false, cancellationToken).ConfigureAwait(false);
    }

    private void ResetHistory()
    {
        _lastApplied = null;
        _previousSensors = new double?[4];
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private static FanChannelConfiguration[] CopyAndValidate(IReadOnlyList<FanChannelConfiguration> channels)
    {
        ArgumentNullException.ThrowIfNull(channels);
        if (channels.Count != 4 || channels.Any(x => x is null))
            throw new ArgumentException("Exactly four fan channel configurations are required.", nameof(channels));
        return channels.Select(c =>
        {
            if (!Enum.IsDefined(c.Mode) || c.MinimumPercent is < 0 or > 100 ||
                c.MaximumPercent < c.MinimumPercent || c.MaximumPercent > 100 ||
                c.FixedPercent is < 0 or > 100 || c.FailSafePercent is < 0 or > 100 ||
                !double.IsFinite(c.HysteresisDegrees) || c.HysteresisDegrees is < 0 or > 50 ||
                c.Curve is null || c.Curve.Any(p => !double.IsFinite(p.Temperature) || p.OutputPercent is < 0 or > 100))
                throw new ArgumentException("Invalid fan safety limits or curve.", nameof(channels));
            return new FanChannelConfiguration
            {
                Mode = c.Mode, SensorKey = c.SensorKey, FixedPercent = c.FixedPercent,
                MinimumPercent = c.MinimumPercent, MaximumPercent = c.MaximumPercent,
                FailSafePercent = c.FailSafePercent, HysteresisDegrees = c.HysteresisDegrees,
                AllowStop = c.AllowStop, Curve = c.Curve.ToList()
            };
        }).ToArray();
    }

    public static bool TryConvertSensor(object? raw, out double? result)
    {
        result = null;
        if (raw is null || raw is bool) return false;
        try
        {
            var number = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
            if (!double.IsFinite(number)) return false;
            result = number;
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed) return;
            _disposed = true;
            IsOutputEnabled = false;
            ResetHistory();
        }
        finally { _gate.Release(); }
    }
}

/// <summary>Deterministic in-memory sensors for headless integration tests and simulations.</summary>
public sealed class SimulatedFanSensors : IFanSensorSource
{
    private readonly object _gate = new();
    private Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);
    public void Set(string key, object? value)
    {
        lock (_gate) _values[key] = value;
    }
    public void Remove(string key)
    {
        lock (_gate) _values.Remove(key);
    }
    public IReadOnlyDictionary<string, object?> Snapshot()
    {
        lock (_gate) return new Dictionary<string, object?>(_values, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>Test-only output sink: captures commands, never touches a serial port.</summary>
public sealed class SimulatedFanOutput : IFanOutputSink
{
    private readonly List<int[]> _writes = new();
    public IReadOnlyList<int[]> Writes => _writes.Select(x => x.ToArray()).ToArray();
    public Task WriteAsync(IReadOnlyList<int> outputs, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (outputs.Count != 4 || outputs.Any(x => x is < 0 or > 100))
            throw new ArgumentException("Invalid four-channel fan output.", nameof(outputs));
        _writes.Add(outputs.ToArray());
        return Task.CompletedTask;
    }
}
