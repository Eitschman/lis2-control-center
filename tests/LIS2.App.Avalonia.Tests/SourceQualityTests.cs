using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class SourceQualityTests
{
    [Theory]
    [InlineData("{Clock.Time|number:no}", "Decimal places must be 0 to 6")]
    [InlineData("{Clock.Time|number:7}", "Decimal places must be 0 to 6")]
    [InlineData("{Clock.Time|number:-1}", "Decimal places must be 0 to 6")]
    [InlineData("{Clock.Time|trim}", "Unknown operation")]
    [InlineData("{Clock.Time|upper:yes}", "Operation takes no argument")]
    [InlineData("{Clock.Time|suffix}", "Missing argument separator")]
    [InlineData("{Clock.Tiem}", "Unknown variable")]
    [InlineData("{Glyph.missing}", "Missing glyph")]
    [InlineData("{Clock.Time", "Unclosed expression")]
    [InlineData("Clock.Time}", "Unexpected brace")]
    [InlineData("{}", "Invalid variable")]
    [InlineData("{Clock.Time|fallbackvar:}", "Invalid variable")]
    public void InvalidExpressionsHaveActionableRanges(string template, string code)
    {
        var issues = TemplateValidation.Check(template, new Dictionary<string, object?> { ["Clock.Time"] = "12:00" }, []);
        Assert.Contains(issues, issue => issue.Code == code && issue.IsError && issue.Start >= 0 && issue.Start + issue.Length <= template.Length);
    }

    [Theory]
    [InlineData("{Clock.Time|number:0|suffix: °C}")]
    [InlineData("{Clock.Time|bytes|prefix: |suffix: }")]
    [InlineData("{Clock.Time|fallbackvar:Clock.Date|fallback:--|upper}")]
    [InlineData("Literal text {Glyph.1}")]
    public void ValidExpressionsRemainValid(string template)
    {
        var values = new Dictionary<string, object?> { ["Clock.Time"] = 21.5, ["Clock.Date"] = "today", ["Glyph.1"] = "x" };
        Assert.Empty(TemplateValidation.Check(template, values, []));
    }

    [Fact]
    public void MissingDiscoveredAndDynamicValuesAreWarningsAndRecover()
    {
        var values = new Dictionary<string, object?> { ["Hardware.cpu"] = null, ["HA.sensor.room"] = "unavailable" };
        var template = "{Hardware.cpu} {HA.sensor.room} {HA.sensor.offline}";
        var issues = TemplateValidation.Check(template, values, ["Hardware.cpu"]);
        Assert.Equal(3, issues.Count);
        Assert.All(issues, issue => Assert.False(issue.IsError));
        Assert.Equal("Not discovered", issues[2].Code);
        values["Hardware.cpu"] = 0; values["HA.sensor.room"] = 21.5; values["HA.sensor.offline"] = false;
        Assert.Empty(TemplateValidation.Check(template, values, []));
        Assert.Contains(TemplateValidation.Check("{Glyph.old}", new Dictionary<string, object?>(), []), issue => issue.IsError);
    }

    [Fact]
    public void NonFiniteAndMissingReadingsDoNotCountAsMeasurements()
    {
        foreach (var value in new object?[] { null, "", "unknown", "unavailable", double.NaN, double.PositiveInfinity, float.NegativeInfinity })
            Assert.False(DataFreshness.IsValidSample(value));
        foreach (var value in new object[] { 0, false, 21.5, " " }) Assert.True(DataFreshness.IsValidSample(value));
    }

    [Fact]
    public void FreshnessTracksTimeoutDisconnectRecoveryAndDoesNotInventSamples()
    {
        var tracker = new DataFreshness();
        var now = DateTimeOffset.Parse("2026-10-29T10:00:00Z");
        Assert.Equal("No data received", tracker.Read("cpu", now, true).State);
        tracker.Observe("cpu", now, false);
        Assert.Null(tracker.Read("cpu", now, false).LastValidAt);
        tracker.Observe("cpu", now, true);
        Assert.Equal("Current", tracker.Read("cpu", now.AddSeconds(9), true, TimeSpan.FromSeconds(10)).State);
        Assert.Equal("Stale", tracker.Read("cpu", now.AddSeconds(10), true, TimeSpan.FromSeconds(10)).State);
        Assert.Equal("Disconnected", tracker.Read("cpu", now.AddSeconds(11), false).State);
        tracker.Observe("cpu", now.AddSeconds(12), false);
        Assert.Equal(now, tracker.Read("cpu", now.AddSeconds(12), false).LastValidAt);
        tracker.Observe("cpu", now.AddSeconds(13), true);
        tracker.Observe("cpu", now, true);
        Assert.Equal("Current", tracker.Read("cpu", now.AddSeconds(14), true, TimeSpan.FromSeconds(10)).State);
        Assert.Equal(1d, tracker.Read("cpu", now.AddSeconds(14), true).AgeSeconds);
        Assert.Equal("Current", tracker.Read("cpu", now.AddDays(3), true).State); // Ereignisquellen besitzen keinen künstlichen Timeout.
    }

    [Theory]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("tr")]
    [InlineData("ru")]
    public void EveryQualityMessageAndHelpHasTranslations(string language)
    {
        foreach (var source in SourceQualityText.Sources)
            Assert.NotEqual(source, SourceQualityText.Get(source, language));
    }
}
