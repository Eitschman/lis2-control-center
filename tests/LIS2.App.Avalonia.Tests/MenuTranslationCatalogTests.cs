using System.Xml.Linq;
using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class MenuTranslationCatalogTests
{
    [Fact]
    public void EveryTranslationPreservesFormattingArguments()
    {
        foreach (var source in UiTranslations.Sources)
        foreach (var language in new[] { "de", "fr", "tr", "ru" })
        {
            var translated = UiTranslations.Translate(source, language);
            Assert.Equal(Placeholders(source), Placeholders(translated));
            // Kopierbare Vorlagenausdrücke sind Literale, keine string.Format-Platzhalter.
            const string templateToken = @"\{[A-Za-z][^{}]*\}";
            var literals = System.Text.RegularExpressions.Regex.Matches(source, templateToken).Select(match => match.Value);
            Assert.All(literals, token => Assert.Contains(token, translated));
            _ = System.Text.CompositeFormat.Parse(System.Text.RegularExpressions.Regex.Replace(translated, templateToken, ""));
        }
    }

    private static string[] Placeholders(string text) => System.Text.RegularExpressions.Regex
        .Matches(text, @"\{\d+(?:,[^}:]+)?(?::[^}]+)?\}")
        .Select(match => match.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();

    [Fact]
    public void NestedMessagesSwitchLanguageAndLiteralBracesAndNumberFormatsSurvive()
    {
        UiMessage safety = " (fail-safe)";
        UiMessage channel = $"Fan {2}: {85}%{safety}";
        UiMessage message = $"DRY RUN: {UiMessage.Join(" / ", new[] { channel })}";
        Assert.Equal("TESTLAUF: Lüfter 2: 85% (Sicherheitswert)", message.Render("de"));
        Assert.Equal("SIMULATION : Ventilateur 2 : 85% (sécurité)", message.Render("fr"));
        Assert.Equal("DRY RUN: Fan 2: 85% (fail-safe)", message.Render("en"));
        UiMessage literal = "{Clock.Time}";
        UiMessage formatted = $"{{Clock.Time}} {42,4:X2}";
        Assert.Equal("{Clock.Time}", literal.Render("de"));
        Assert.Equal("{Clock.Time}   2A", formatted.Render("de"));
    }

    [Theory]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("tr")]
    [InlineData("ru")]
    public void AllMainWindowLabelsHaveExplicitTranslations(string language)
    {
        using var stream = typeof(MenuTranslationCatalogTests).Assembly.GetManifestResourceStream("MainWindow.axaml")!;
        var source = XDocument.Load(stream);
        var invariants = new HashSet<string>(StringComparer.Ordinal)
        {
            "CPU", "GPU", "URL", "LIS2 CONTROL CENTER", "LIS2 Control Center", "Windows",
            "Windows Media Player", "Windows Media Player Legacy", "Winamp", "Home Assistant",
            "80-A7", "Ä Ö Ü  ä ö ü  ß  °  µ  ±  £  ×  ÷  ← →  Ω π Σ √ ∞", "http://homeassistant.local:8123"
        };
        var missing = source.Descendants()
            .SelectMany(element => element.Attributes().Where(attribute =>
                attribute.Name.LocalName is "Content" or "ToolTip.Tip" or "Header" or "Watermark" ||
                attribute.Name.LocalName == "Text" && element.Name.LocalName != "TextBox"))
            .Select(attribute => attribute.Value)
            .Where(text => text.Length > 1 && !text.StartsWith('{') && text.Any(char.IsLetter) && !invariants.Contains(text))
            .Distinct().Where(text => !UiTranslations.HasTranslation(text, language)).OrderBy(text => text).ToArray();
        Assert.True(missing.Length == 0, language + ": missing UI translations: " + string.Join(" | ", missing));
    }

    [Fact]
    public void MessageKeepsDataOpaqueAndRendersAgainInAnotherLanguage()
    {
        const string userData = "Connected";
        UiMessage message = $"Serial: {userData}";
        Assert.Equal("Serial: Connected", message.Render("en"));
        var connected = true;
        UiMessage conditional = connected ? (UiMessage)$"Serial: {userData}" : "Disconnected";
        Assert.Equal("Seriell: Connected", conditional.Render("de"));
        Assert.Equal("Seriell: Connected", message.Render("de"));
        Assert.Equal("Serial: Connected", message.Render("en"));
    }
}
