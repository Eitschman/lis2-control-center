using LibreHardwareMonitor.Hardware;

namespace LIS2.Sources;

public sealed class LibreHardwareMonitorDataSource : IDataSource
{
    private readonly Dictionary<string, object?> _values =
        new(StringComparer.OrdinalIgnoreCase);

    private Computer? _computer;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public string Id => "Hardware";

    public IReadOnlyDictionary<string, object?> Values => _values;

    public event EventHandler? Changed;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_loopTask is not null)
            return Task.CompletedTask;

        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true,
            IsNetworkEnabled = true,
            IsControllerEnabled = true
        };

        _computer.Open();

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopTask = RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);

            if (_loopTask is not null)
            {
                try
                {
                    await _loopTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            _cts.Dispose();
            _cts = null;
            _loopTask = null;
        }

        if (_computer is not null)
        {
            _computer.Close();
            _computer = null;
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (!cancellationToken.IsCancellationRequested)
        {
            Refresh();

            if (!await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                break;
        }
    }

    private void Refresh()
    {
        if (_computer is null)
            return;

        var next = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var hardware in _computer.Hardware)
            ReadHardware(hardware, next);

        _values.Clear();

        foreach (var pair in next)
            _values[pair.Key] = pair.Value;

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static void ReadHardware(
        IHardware hardware,
        IDictionary<string, object?> values)
    {
        hardware.Update();

        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.Value is null)
                continue;

            var key = CreateKey(hardware, sensor);
            values[key] = sensor.Value.Value;
            values[$"{key}.Unit"] = GetUnit(sensor.SensorType);
        }

        foreach (var child in hardware.SubHardware)
            ReadHardware(child, values);
    }

    private static string CreateKey(IHardware hardware, ISensor sensor)
    {
        var hardwareName = Normalize(hardware.Name);
        var sensorName = Normalize(sensor.Name);
        return $"{hardwareName}.{sensor.SensorType}.{sensorName}";
    }

    private static string Normalize(string value)
    {
        var chars = value
            .Select(character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray();

        return new string(chars)
            .Trim('_')
            .Replace("__", "_", StringComparison.Ordinal);
    }

    private static string GetUnit(SensorType type) =>
        type switch
        {
            SensorType.Temperature => "C",
            SensorType.Load => "%",
            SensorType.Fan => "RPM",
            SensorType.Clock => "MHz",
            SensorType.Voltage => "V",
            SensorType.Power => "W",
            SensorType.Data => "GB",
            SensorType.SmallData => "MB",
            SensorType.Throughput => "B/s",
            _ => string.Empty
        };

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
