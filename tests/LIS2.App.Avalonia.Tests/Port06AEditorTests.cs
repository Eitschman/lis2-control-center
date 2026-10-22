using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class Port06AEditorTests
{
    [Fact]
    public void CurveRoundTripPreservesPrecisionAndNormalizesDuplicateTemperatures()
    {
        var points = FanCurveEditor.Parse("60=80\n20.123456789012345=30\n60=95");
        Assert.Equal(2, points.Count);
        Assert.Equal(95, points[1].OutputPercent);
        Assert.Equal(points, FanCurveEditor.Parse(FanCurveEditor.Format(points)));
        Assert.Empty(FanCurveEditor.Parse(""));
    }

    [Theory]
    [InlineData("NaN=30")]
    [InlineData("Infinity=30")]
    [InlineData("20=101")]
    [InlineData("20=-1")]
    [InlineData("20,5=30")]
    [InlineData("20 30")]
    public void InvalidCurveIsRejected(string text) => Assert.Throws<FormatException>(() => FanCurveEditor.Parse(text));

    [Fact]
    public void FailureLogRetainsTechnicalTraceWithoutExceptionSecretsAndReportsWriteFailure()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-port06a-log-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "errors.log");
            Exception failure;
            try { throw new InvalidOperationException("secret-token-must-not-be-logged"); }
            catch (Exception exception) { failure = exception; }
            Assert.True(FailureLog.Write("TestStartup", failure, path));
            Assert.True(FailureLog.Write("TestCrash", failure, path));
            var text = File.ReadAllText(path);
            Assert.Contains("TestStartup", text);
            Assert.Contains("TestCrash", text);
            Assert.Contains(nameof(InvalidOperationException), text);
            Assert.Contains(nameof(FailureLogRetainsTechnicalTraceWithoutExceptionSecretsAndReportsWriteFailure), text);
            Assert.DoesNotContain("secret-token", text);
            Assert.False(FailureLog.Write("TestFailure", failure, directory));
        }
        finally { Directory.Delete(directory, true); }
    }
}
