using LIS2.Fans;

namespace LIS2.Fans.Tests;

public sealed class FanControllerTests
{
    private readonly FanController _controller = new();

    [Fact]
    public void FixedMode_IsClampedToMinimum()
    {
        var configuration = new FanChannelConfiguration
        {
            Mode = FanMode.Fixed,
            FixedPercent = 10,
            MinimumPercent = 30,
            MaximumPercent = 100
        };

        Assert.Equal(30, _controller.CalculateOutput(configuration));
    }

    [Fact]
    public void InvalidSensor_UsesFailSafe()
    {
        var configuration = new FanChannelConfiguration
        {
            Mode = FanMode.Curve,
            FailSafePercent = 100
        };

        Assert.Equal(
            100,
            _controller.CalculateOutput(configuration, 50, sensorValid: false));
    }

    [Fact]
    public void CurveMode_Interpolates()
    {
        var configuration = new FanChannelConfiguration
        {
            Mode = FanMode.Curve,
            MinimumPercent = 30,
            Curve =
            {
                new FanCurvePoint(40, 40),
                new FanCurvePoint(60, 80)
            }
        };

        Assert.Equal(60, _controller.CalculateOutput(configuration, 50));
    }

    [Fact]
    public void OffMode_DoesNotStopUnlessExplicitlyAllowed()
    {
        var configuration = new FanChannelConfiguration
        {
            Mode = FanMode.Off,
            MinimumPercent = 30,
            AllowStop = false
        };

        Assert.Equal(30, _controller.CalculateOutput(configuration));
    }

    [Fact]
    public void OffMode_CanStopWhenExplicitlyAllowed()
    {
        var configuration = new FanChannelConfiguration
        {
            Mode = FanMode.Off,
            MinimumPercent = 30,
            AllowStop = true
        };

        Assert.Equal(0, _controller.CalculateOutput(configuration));
    }
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonFiniteSensorValue_UsesFailSafe(double value)
    {
        var configuration = new FanChannelConfiguration
        {
            Mode = FanMode.Curve,
            MinimumPercent = 30,
            MaximumPercent = 100,
            FailSafePercent = 85,
            Curve =
            {
                new FanCurvePoint(40, 40),
                new FanCurvePoint(60, 80)
            }
        };

        Assert.Equal(85, _controller.CalculateOutput(configuration, value));
    }

    [Fact]
    public void ExternalMode_IsClampedToConfiguredRange()
    {
        var configuration = new FanChannelConfiguration
        {
            Mode = FanMode.External,
            MinimumPercent = 30,
            MaximumPercent = 80
        };

        Assert.Equal(80, _controller.CalculateOutput(configuration, externalPercent: 500));
        Assert.Equal(30, _controller.CalculateOutput(configuration, externalPercent: -10));
    }

    [Fact]
    public void FailSafe_IsClampedAndNeverStopsFan()
    {
        var configuration = new FanChannelConfiguration
        {
            Mode = FanMode.Curve,
            MinimumPercent = 30,
            MaximumPercent = 80,
            FailSafePercent = 0,
            AllowStop = true
        };

        Assert.Equal(
            30,
            _controller.CalculateOutput(configuration, sensorValue: null, sensorValid: false));
    }

    [Fact]
    public void CurveMode_HysteresisKeepsPreviousOutputForSmallSensorChange()
    {
        var configuration = new FanChannelConfiguration
        {
            Mode = FanMode.Curve,
            MinimumPercent = 30,
            HysteresisDegrees = 2,
            Curve =
            {
                new FanCurvePoint(40, 40),
                new FanCurvePoint(60, 80)
            }
        };

        Assert.Equal(
            60,
            _controller.CalculateOutput(
                configuration,
                sensorValue: 50.9,
                previousSensorValue: 50,
                previousOutputPercent: 60));
    }

    [Fact]
    public void CurveMode_HysteresisRecalculatesAfterThreshold()
    {
        var configuration = new FanChannelConfiguration
        {
            Mode = FanMode.Curve,
            MinimumPercent = 30,
            HysteresisDegrees = 1,
            Curve =
            {
                new FanCurvePoint(40, 40),
                new FanCurvePoint(60, 80)
            }
        };

        Assert.Equal(
            64,
            _controller.CalculateOutput(
                configuration,
                sensorValue: 52,
                previousSensorValue: 50,
                previousOutputPercent: 60));
    }
}
