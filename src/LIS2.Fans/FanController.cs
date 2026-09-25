namespace LIS2.Fans;

public sealed class FanController
{
    public int CalculateOutput(
        FanChannelConfiguration configuration,
        double? sensorValue = null,
        int? externalPercent = null,
        bool sensorValid = true,
        double? previousSensorValue = null,
        int? previousOutputPercent = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (!sensorValid || !IsFinite(sensorValue))
            return Clamp(configuration.FailSafePercent, configuration, allowZero: false);

        if (configuration.Mode == FanMode.Curve &&
            sensorValue is not null &&
            previousSensorValue is not null &&
            previousOutputPercent is not null &&
            configuration.HysteresisDegrees > 0 &&
            Math.Abs(sensorValue.Value - previousSensorValue.Value) < configuration.HysteresisDegrees)
        {
            return Clamp(previousOutputPercent.Value, configuration, configuration.AllowStop);
        }

        var output = configuration.Mode switch
        {
            FanMode.Fixed => configuration.FixedPercent,
            FanMode.Curve => CalculateCurve(configuration, sensorValue),
            FanMode.Follow => sensorValue is null
                ? configuration.FailSafePercent
                : (int)Math.Round(sensorValue.Value, MidpointRounding.AwayFromZero),
            FanMode.External => externalPercent ?? configuration.FailSafePercent,
            FanMode.Off => configuration.AllowStop ? 0 : configuration.MinimumPercent,
            _ => configuration.FailSafePercent
        };

        return Clamp(output, configuration, configuration.AllowStop);
    }

    private static int CalculateCurve(
        FanChannelConfiguration configuration,
        double? sensorValue)
    {
        if (!IsFinite(sensorValue) || configuration.Curve.Count == 0)
            return configuration.FailSafePercent;

        var points = configuration.Curve
            .OrderBy(point => point.Temperature)
            .ToArray();

        if (sensorValue <= points[0].Temperature)
            return points[0].OutputPercent;

        if (sensorValue >= points[^1].Temperature)
            return points[^1].OutputPercent;

        for (var index = 0; index < points.Length - 1; index++)
        {
            var lower = points[index];
            var upper = points[index + 1];

            if (sensorValue < lower.Temperature || sensorValue > upper.Temperature)
                continue;

            var range = upper.Temperature - lower.Temperature;
            if (range <= 0)
                return upper.OutputPercent;

            var ratio = (sensorValue.Value - lower.Temperature) / range;
            var interpolated =
                lower.OutputPercent +
                ((upper.OutputPercent - lower.OutputPercent) * ratio);

            return (int)Math.Round(interpolated, MidpointRounding.AwayFromZero);
        }

        return configuration.FailSafePercent;
    }

    private static bool IsFinite(double? value) =>
        value is null || (!double.IsNaN(value.Value) && !double.IsInfinity(value.Value));

    private static int Clamp(
        int value,
        FanChannelConfiguration configuration,
        bool allowZero)
    {
        var minimum = Math.Clamp(configuration.MinimumPercent, 0, 100);
        var maximum = Math.Clamp(configuration.MaximumPercent, minimum, 100);

        if (allowZero && value <= 0)
            return 0;

        return Math.Clamp(value, minimum, maximum);
    }
}
