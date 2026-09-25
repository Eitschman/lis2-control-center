using LIS2.Display;

namespace LIS2.Display.Tests;

public sealed class VisibilityEvaluatorTests
{
    private readonly VisibilityEvaluator _evaluator = new();

    [Theory]
    [InlineData("State=Playing", true)]
    [InlineData("State!=Stopped", true)]
    [InlineData("State=Stopped", false)]
    [InlineData("State!=Playing", false)]
    public void StringComparisons_Work(string expression, bool expected)
    {
        var values = new Dictionary<string, object?>
        {
            ["State"] = "Playing"
        };

        Assert.Equal(expected, _evaluator.IsVisible(expression, values));
    }

    [Theory]
    [InlineData("Temp>20", true)]
    [InlineData("Temp>=21.5", true)]
    [InlineData("Temp<22", true)]
    [InlineData("Temp<=21.5", true)]
    [InlineData("Temp>30", false)]
    public void NumericComparisons_Work(string expression, bool expected)
    {
        var values = new Dictionary<string, object?>
        {
            ["Temp"] = 21.5
        };

        Assert.Equal(expected, _evaluator.IsVisible(expression, values));
    }

    [Theory]
    [InlineData("Key>=10", "Key", ">=", "10")]
    [InlineData("Key!=off", "Key", "!=", "off")]
    [InlineData("HA.sensor.x<5", "HA.sensor.x", "<", "5")]
    public void TryParse_ReturnsParts(
        string expression,
        string key,
        string op,
        string value)
    {
        Assert.True(
            VisibilityEvaluator.TryParse(
                expression,
                out var actualKey,
                out var actualOperator,
                out var actualValue));

        Assert.Equal(key, actualKey);
        Assert.Equal(op, actualOperator);
        Assert.Equal(value, actualValue);
    }
}
