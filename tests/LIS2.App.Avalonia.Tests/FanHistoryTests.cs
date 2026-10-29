using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class FanHistoryTests
{
    [Fact]
    public void RetainsOneHourAndRejectsOutOfOrderSamples()
    {
        var history = new FanHistory();
        var start = DateTimeOffset.UtcNow;
        for (var index = 0; index < 4000; index++)
            history.Add(0, "cpu", new(start.AddSeconds(index * 2), index, 50, 900));
        var end = start.AddSeconds(7998);
        var samples = history.Read(0, end, TimeSpan.FromHours(2));
        Assert.Equal(1801, samples.Length);
        Assert.Equal(end - TimeSpan.FromHours(1), samples[0].Time);
        history.Add(0, "cpu", new(start, -1, 50, 900));
        Assert.Equal(samples, history.Read(0, end, TimeSpan.FromHours(2)));
        Assert.Equal(151, history.Read(0, end, TimeSpan.FromMinutes(5)).Length);
        Assert.Empty(history.Read(1, end, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void PreservesMissingSamplesAndZeroAndSeparatesChangedSources()
    {
        var history = new FanHistory();
        var now = DateTimeOffset.UtcNow;
        history.Add(0, "cpu", new(now, 42, 40, 0));
        history.Add(0, "cpu", new(now.AddSeconds(2), double.NaN, double.PositiveInfinity, -1));
        history.Add(0, "cpu", new(now.AddSeconds(4), 43, 42, 1000));
        var samples = history.Read(0, now.AddSeconds(4), TimeSpan.FromMinutes(5));
        Assert.Equal(0, samples[0].Rpm);
        Assert.Null(samples[1].Sensor); Assert.Null(samples[1].Target); Assert.Null(samples[1].Rpm);
        Assert.Equal(43, samples[2].Sensor);
        history.Add(0, "gpu", new(now.AddSeconds(6), 60, 80, null));
        Assert.Equal(60, Assert.Single(history.Read(0, now.AddSeconds(6), TimeSpan.FromHours(1))).Sensor);
        Assert.Empty(new FanHistory().Read(0, now, TimeSpan.FromHours(1)));
    }
}
