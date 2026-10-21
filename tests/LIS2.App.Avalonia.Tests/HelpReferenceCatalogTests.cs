using LIS2.App.Avalonia;
using Xunit;

namespace LIS2.App.Avalonia.Tests;

public class HelpReferenceCatalogTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("tr")]
    [InlineData("ru")]
    public void EveryHelpSectionHasTranslationsAndPreservesExecutableExamples(string language)
    {
        for (var section = 0; section < HelpReferenceCatalog.Sections.Length; section++)
        {
            Assert.NotEmpty(HelpReferenceLocalization.Translate(HelpReferenceCatalog.Sections[section], language));
            var english = HelpReferenceCatalog.GetSection(section, ["Clock.Time", "Hardware.Custom_Key"], ["My glyph"], "en");
            var translated = HelpReferenceCatalog.GetSection(section, ["Clock.Time", "Hardware.Custom_Key"], ["My glyph"], language);
            Assert.Equal(english.Count, translated.Count);
            Assert.All(translated, entry => { Assert.NotEmpty(entry.Heading); Assert.NotEmpty(entry.Body); });
            // Die Beispiele sollen direkt kopierbar bleiben, auch bei selbst vergebenen Schlüsseln.
            for (var index = 0; index < english.Count; index++)
            {
                var tokens = System.Text.RegularExpressions.Regex.Matches(english[index].Body, @"\{[^{}]+\}")
                    .Select(match => match.Value);
                Assert.All(tokens, token => Assert.Contains(token, translated[index].Body));
            }
            Assert.NotEmpty(HelpReferenceCatalog.GetSection(section, [], [], language));
        }
    }

    [Fact]
    public void AllWpfSectionsHaveNavigableContent()
    {
        Assert.Equal(10, HelpReferenceCatalog.Sections.Length);
        for (var i = 0; i < HelpReferenceCatalog.Sections.Length; i++)
        {
            var entries = HelpReferenceCatalog.GetSection(i, Array.Empty<string>(), Array.Empty<string>());
            Assert.NotEmpty(entries);
            Assert.All(entries, entry =>
            {
                Assert.False(string.IsNullOrWhiteSpace(entry.Heading));
                Assert.False(string.IsNullOrWhiteSpace(entry.Body));
            });
        }
    }

    [Theory]
    [InlineData(-20, 0)]
    [InlineData(0, 0)]
    [InlineData(9, 9)]
    [InlineData(99, 9)]
    public void ContextNavigationClampsToAvailableSections(int requested, int expected) =>
        Assert.Equal(expected, HelpReferenceCatalog.ClampSection(requested));

    [Fact]
    public void PagesHelpListsLiveKeysButAvoidsThousandsOfHaEntities()
    {
        var keys = new[] { "Clock.Time", "Hardware.Cpu.Load", "Clock.Time" }
            .Concat(Enumerable.Range(0, 3000).Select(i => "HA.sensor." + i));
        var text = string.Join("\n", HelpReferenceCatalog.GetSection(2, keys, []).Select(x => x.Body));
        Assert.Contains("{Clock.Time}", text);
        Assert.Contains("{Hardware.Cpu.Load}", text);
        Assert.DoesNotContain("{HA.sensor.2999}", text);
        var runtimeList = HelpReferenceCatalog.GetSection(2, keys, [])
            .Single(entry => entry.Heading == "Available runtime variables").Body;
        Assert.Equal(1, runtimeList.Split("{Clock.Time}").Length - 1);
    }

    [Fact]
    public void DisplayHelpProvidesRunnableNumericAndNamedGlyphKeys()
    {
        var text = string.Join("\n", HelpReferenceCatalog.GetSection(1, [], ["CPU temp!", "Fan-1"])
            .Select(x => x.Body));
        Assert.Contains("CPU temp!: {Glyph.1}", text);
        Assert.Contains("Fan-1: {Glyph.2}", text);
        Assert.Contains("{Glyph.CPU_temp}", text);
        Assert.Contains("{Glyph.8}", text);
        var source = new PageSourceValues();
        source.SetGlyphNames(["CPU temp!", "Fan-1"]);
        var values = source.Snapshot(DateTimeOffset.UnixEpoch);
        var examples = HelpReferenceCatalog.GetSection(1, [], ["CPU temp!", "Fan-1"])
            .Where(entry => entry.Heading == "Named glyph");
        foreach (var example in examples)
        {
            var template = example.Body.Contains(": ", StringComparison.Ordinal)
                ? example.Body[(example.Body.IndexOf(": ", StringComparison.Ordinal) + 2)..] : example.Body;
            Assert.Single(new LIS2.Display.TemplateRenderer().Render(template, values));
        }
    }

    [Fact]
    public void DocumentedHaFormattingAndVisibilityWorkWithActualEngines()
    {
        var values = new Dictionary<string, object?>
        {
            ["HA.sensor.temperature"] = 26.5,
            ["HA.binary_sensor.window"] = "on"
        };
        var examples = HelpReferenceCatalog.GetSection(9, [], []);
        var renderer = new LIS2.Display.TemplateRenderer();
        var format = examples.Single(entry => entry.Heading == "Formatting").Body;
        Assert.Equal(26.5.ToString("F1") + "°C", renderer.Render(format, values));
        Assert.Equal("--", renderer.Render(format, new Dictionary<string, object?>()));
        var visibility = new LIS2.Display.VisibilityEvaluator();
        foreach (var expression in examples.Single(entry => entry.Heading == "Visibility").Body.Split("  /  "))
        {
            Assert.True(visibility.IsVisible(expression, values));
            Assert.False(visibility.IsVisible(expression, new Dictionary<string, object?>()));
        }
        Assert.Equal(0.5.ToString("F1") + "%", renderer.Render("{value|percent:1}", new Dictionary<string, object?> { ["value"] = 0.5 }));
        Assert.Equal("1 KB", renderer.Render("{value|bytes:0}", new Dictionary<string, object?> { ["value"] = 1024 }));
    }

    [Fact]
    public void HomeAssistantHelpDocumentsStateAttributesAndFallback()
    {
        var text = string.Join("\n", HelpReferenceCatalog.GetSection(9, [], []).Select(x => x.Body));
        Assert.Contains(".attribute.media_title", text);
        Assert.Contains(".FriendlyName", text);
        Assert.Contains("|fallback:", text);
        Assert.Contains("HA.binary_sensor.window=on", text);
    }
}
