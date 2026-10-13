using LIS2.Fans;
using LIS2.Runtime;

namespace LIS2.App.Avalonia.Tests;

public sealed class FanRuntimeTests
{
    private static FanChannelConfiguration[] Config(FanMode mode = FanMode.Fixed) =>
        Enumerable.Range(0, 4).Select(_ => new FanChannelConfiguration
        {
            Mode = mode, SensorKey = "CPU", FixedPercent = 60,
            MinimumPercent = 30, MaximumPercent = 90, FailSafePercent = 85,
            HysteresisDegrees = 2, Curve = new List<FanCurvePoint>
            {
                new(40, 40), new(60, 80)
            }
        }).ToArray();

    [Fact]
    public async Task DefaultsToDryRunWithoutWritingHardware()
    {
        var sensors = new SimulatedFanSensors();
        var sink = new SimulatedFanOutput();
        await using var runtime = new FanRuntime(sensors, sink, Config());
        var state = await runtime.TickAsync();
        Assert.False(state.Applied);
        Assert.Empty(sink.Writes);
        Assert.All(state.Channels, c => Assert.Equal(60, c.OutputPercent));
    }

    [Fact]
    public async Task FourChannelsWriteOnceUntilOutputsChange()
    {
        var sensors = new SimulatedFanSensors();
        var sink = new SimulatedFanOutput();
        await using var runtime = new FanRuntime(sensors, sink, Config());
        await runtime.SetOutputEnabledAsync(true);
        Assert.True((await runtime.TickAsync()).Applied);
        Assert.False((await runtime.TickAsync()).Applied);
        Assert.Single(sink.Writes);
        Assert.Equal(new[] { 60, 60, 60, 60 }, sink.Writes[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public async Task InvalidSensorsFailSafeWithoutStoppingFan(object? value)
    {
        var sensors = new SimulatedFanSensors();
        sensors.Set("CPU", value);
        var sink = new SimulatedFanOutput();
        await using var runtime = new FanRuntime(sensors, sink, Config(FanMode.Curve));
        await runtime.SetOutputEnabledAsync(true);
        var state = await runtime.TickAsync();
        Assert.All(state.Channels, c => { Assert.False(c.SensorValid); Assert.Equal(85, c.OutputPercent); });
        Assert.Equal(85, sink.Writes.Single()[0]);
    }

    [Fact]
    public async Task CurveHysteresisAndThresholdMatchFanController()
    {
        var sensors = new SimulatedFanSensors();
        var sink = new SimulatedFanOutput();
        sensors.Set("CPU", 50d);
        await using var runtime = new FanRuntime(sensors, sink, Config(FanMode.Curve));
        await runtime.SetOutputEnabledAsync(true);
        await runtime.TickAsync();
        Assert.Equal(60, sink.Writes[^1][0]);
        sensors.Set("CPU", 50.5d);
        Assert.False((await runtime.TickAsync()).Applied);
        sensors.Set("CPU", 53d);
        Assert.True((await runtime.TickAsync()).Applied);
        Assert.Equal(66, sink.Writes[^1][0]);
    }

    [Theory]
    [InlineData(FanMode.Fixed, 60)]
    [InlineData(FanMode.External, 85)]
    [InlineData(FanMode.Off, 30)]
    public async Task OtherModesUseExistingController(FanMode mode, int expected)
    {
        var sensors = new SimulatedFanSensors();
        var sink = new SimulatedFanOutput();
        await using var runtime = new FanRuntime(sensors, sink, Config(mode));
        await runtime.SetOutputEnabledAsync(true);
        await runtime.TickAsync();
        Assert.Equal(expected, sink.Writes.Single()[0]);
    }

    [Fact]
    public async Task DisconnectRequiresExplicitReenableAndForcesNewWrite()
    {
        var sensors = new SimulatedFanSensors();
        var sink = new SimulatedFanOutput();
        await using var runtime = new FanRuntime(sensors, sink, Config());
        await runtime.SetOutputEnabledAsync(true);
        await runtime.TickAsync();
        await runtime.DisconnectAsync();
        Assert.False((await runtime.TickAsync()).Applied);
        Assert.Single(sink.Writes);
        await runtime.SetOutputEnabledAsync(true);
        Assert.True((await runtime.TickAsync()).Applied);
        Assert.Equal(2, sink.Writes.Count);
    }

    [Fact]
    public async Task ConfigurationCopiesInputsAndRejectsUnsafeRanges()
    {
        var sensors = new SimulatedFanSensors();
        var sink = new SimulatedFanOutput();
        var channels = Config();
        await using var runtime = new FanRuntime(sensors, sink, channels);
        channels[0].FixedPercent = 10;
        Assert.Equal(60, (await runtime.TickAsync()).Channels[0].OutputPercent);
        channels[0].MinimumPercent = 101;
        await Assert.ThrowsAsync<ArgumentException>(() => runtime.ConfigureAsync(channels));
        Assert.Equal(60, (await runtime.TickAsync()).Channels[0].OutputPercent);
    }

    [Fact]
    public async Task FailedWriteDoesNotAdvanceHistoryAndIsRetried()
    {
        var sensors = new SimulatedFanSensors();
        var sink = new FailingSink();
        await using var runtime = new FanRuntime(sensors, sink, Config());
        await runtime.SetOutputEnabledAsync(true);
        await Assert.ThrowsAsync<IOException>(() => runtime.TickAsync());
        Assert.Null(runtime.LastApplied);
        Assert.True((await runtime.TickAsync()).Applied);
        Assert.Equal(2, sink.Calls);
    }

    [Fact]
    public async Task ConcurrentTicksAreSerialized()
    {
        var sensors = new SimulatedFanSensors();
        var sink = new BlockingSink();
        await using var runtime = new FanRuntime(sensors, sink, Config());
        await runtime.SetOutputEnabledAsync(true);
        var first = runtime.TickAsync();
        await sink.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = runtime.TickAsync();
        Assert.False(second.IsCompleted);
        sink.Release.SetResult(true);
        await Task.WhenAll(first, second);
        Assert.Equal(1, sink.Calls);
    }

    private sealed class FailingSink : IFanOutputSink
    {
        public int Calls { get; private set; }
        public Task WriteAsync(IReadOnlyList<int> values, CancellationToken token)
        {
            if (++Calls == 1) throw new IOException("simulated failure");
            return Task.CompletedTask;
        }
    }

    private sealed class BlockingSink : IFanOutputSink
    {
        public int Calls { get; private set; }
        public TaskCompletionSource<bool> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task WriteAsync(IReadOnlyList<int> values, CancellationToken token)
        {
            Calls++;
            Entered.TrySetResult(true);
            await Release.Task;
        }
    }
}
