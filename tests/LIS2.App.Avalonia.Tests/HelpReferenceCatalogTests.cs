using LIS2.App.Avalonia;
using Xunit;

namespace LIS2.App.Avalonia.Tests;

public class HelpReferenceCatalogTests
{
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
        Assert.Equal(1, text.Split("{Clock.Time}").Length - 1);
    }

    [Fact]
    public void DisplayHelpUsesNormalizedNamedGlyphKeys()
    {
        var text = string.Join("\n", HelpReferenceCatalog.GetSection(1, [], ["CPU temp!", "Fan-1"])
            .Select(x => x.Body));
        Assert.Contains("{Glyph.CPU_temp}", text);
        Assert.Contains("{Glyph.Fan-1}", text);
        Assert.Contains("{Glyph.8}", text);
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
