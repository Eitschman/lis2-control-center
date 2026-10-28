using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class HelpLanguageUiTests
{
    [AvaloniaTheory]
    [InlineData("German", "Hilfe", "Die schreibgeschützte WebSocket-Integration lädt Zustände und empfängt state_changed-Aktualisierungen.")]
    [InlineData("English", "Help", "The read-only WebSocket integration loads states and receives live state_changed updates.")]
    [InlineData("French", "Aide", "L’intégration WebSocket en lecture seule charge les états et reçoit les mises à jour state_changed en direct.")]
    [InlineData("Turkish", "Yardım", "Salt okunur WebSocket entegrasyonu durumları yükler ve canlı state_changed güncellemelerini alır.")]
    [InlineData("Russian", "Справка", "Интеграция WebSocket в режиме чтения загружает состояния и получает обновления state_changed в реальном времени.")]
    [InlineData("System", "Hilfe", "Die schreibgeschützte WebSocket-Integration lädt Zustände und empfängt state_changed-Aktualisierungen.")]
    public async Task SavedPreferenceLocalizesHelpAndOpenDialogFollowsLanguageChanges(string preference, string title, string explanation)
    {
        var originalCulture = System.Globalization.CultureInfo.CurrentUICulture;
        if (preference == "System") System.Globalization.CultureInfo.CurrentUICulture = new("de-DE");
        var directory = Path.Combine(Path.GetTempPath(), "lis2-help-" + Guid.NewGuid().ToString("N"));
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        await store.SaveAsync(new PortableWorkspace { LanguageMode = preference });
        var window = new MainWindow(store, new PortableHaTokenStore(directory, "lis2-no-secret-service"), false,
            _ => throw new InvalidOperationException("No serial hardware in help tests."));
        try
        {
            window.Show();
            Click(window, "NavButton9");
            Click(window, "OpenHelp");
            var help = window.OwnedWindows.Single(dialog => dialog.Name == "HelpDialog");
            Assert.Equal(title, help.Title);
            Assert.Equal(HelpReferenceLocalization.Translate("Help & reference", preference switch
            {
                "German" or "System" => "de", "French" => "fr", "Turkish" => "tr", "Russian" => "ru", _ => "en"
            }), window.FindControl<Button>("OpenHelp")!.Content);
            Assert.Contains(help.GetVisualDescendants().OfType<TextBlock>(), text =>
                text.Text == explanation);
            Assert.Contains(help.GetVisualDescendants().OfType<TextBlock>(), text =>
                text.Text == "{HA.media_player.living_room.attribute.media_title}");
            var helpSections = help.GetVisualDescendants().OfType<ListBox>().Single();
            for (var section = 0; section < 10; section++)
            {
                helpSections.SelectedIndex = section;
                var paragraphs = help.GetVisualDescendants().OfType<TextBlock>().ToArray();
                var entries = HelpReferenceCatalog.GetSection(section, [], [], preference switch
                {
                    "German" or "System" => "de", "French" => "fr", "Turkish" => "tr", "Russian" => "ru", _ => "en"
                });
                foreach (var entry in entries)
                {
                    if (entry.Body == HelpReferenceLocalization.Translate("No runtime variables currently available.", preference switch
                        {
                            "German" or "System" => "de", "French" => "fr", "Turkish" => "tr", "Russian" => "ru", _ => "en"
                        })) continue;
                    var paragraph = paragraphs.FirstOrDefault(text => text.Text == entry.Body);
                    Assert.NotNull(paragraph);
                    Assert.Equal(entry.IsExample
                        ? new FontFamily("Consolas,DejaVu Sans Mono,Liberation Mono")
                        : FontFamily.Default, paragraph.FontFamily);
                }
            }
            helpSections.SelectedIndex = 0;
            help.UpdateLayout();
            var liveValues = help.GetVisualDescendants().OfType<TextBlock>()
                .Single(text => text.Text?.Contains("{Hardware.Cpu.Temperature|number:1|suffix: °C}") == true);
            Assert.Equal(FontFamily.Default, liveValues.FontFamily);
            liveValues.BringIntoView();
            var helpScroll = liveValues.GetVisualAncestors().OfType<ScrollViewer>().First();
            helpScroll.Offset = new global::Avalonia.Vector(0, helpScroll.Extent.Height);
            await Task.Delay(50);
            help.UpdateLayout();
            var typographyScreenshots = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../TestResults/screenshots"));
            Directory.CreateDirectory(typographyScreenshots);
            using (var frame = help.CaptureRenderedFrame())
            {
                Assert.NotNull(frame);
                frame.Save(Path.Combine(typographyScreenshots, "help-typography-" + preference + ".png"));
            }
            helpSections.SelectedIndex = 9;
            help.Width = 840;
            help.Height = 600;
            await Task.Delay(50);
            help.UpdateLayout();
            var screenshots = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../TestResults/screenshots"));
            Directory.CreateDirectory(screenshots);
            using (var frame = help.CaptureRenderedFrame())
            {
                Assert.NotNull(frame);
                frame.Save(Path.Combine(screenshots, "help-" + preference + ".png"));
            }
            var language = window.FindControl<ComboBox>("LanguageModeComboBox")!;
            language.SelectedIndex = 1;
            Assert.Equal("Help", help.Title);
            language.SelectedIndex = 2;
            Assert.Equal("Hilfe", help.Title);
            var sections = help.GetVisualDescendants().OfType<ListBox>().Single();
            Assert.Equal(9, sections.SelectedIndex);
            Assert.Equal("Schließen", help.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "HelpClose").Content);
            Assert.Equal("Hilfe und Referenz", window.FindControl<Button>("OpenHelp")!.Content);
            for (var index = 0; index < 10; index++)
            {
                sections.SelectedIndex = index;
                Assert.Contains(help.GetVisualDescendants().OfType<TextBlock>(), text =>
                    text.Text == HelpReferenceLocalization.Translate(HelpReferenceCatalog.Sections[index], "de"));
            }
            language.SelectedIndex = preference == "French" ? 3 : preference == "Russian" ? 5 : 2;
            sections.SelectedIndex = 2;
            await Task.Delay(50);
            help.UpdateLayout();
            var example = help.GetVisualDescendants().OfType<TextBlock>().Single(text =>
                text.Text?.StartsWith("{Value|number:2}:") == true);
            Assert.Contains("50.1234567 → 50,12", example.Text);
            Assert.Contains(help.GetVisualDescendants().OfType<TextBlock>(), text =>
                text.Text?.Contains("{Value|fallback:--|number:2|suffix:°C}") == true);
            example.BringIntoView();
            await Task.Delay(50);
            help.UpdateLayout();
            using (var frame = help.CaptureRenderedFrame())
            {
                Assert.NotNull(frame);
                frame.Save(Path.Combine(screenshots, "help-formatting-" + preference + ".png"));
            }
            var replacementExample = help.GetVisualDescendants().OfType<TextBlock>().Single(text =>
                text.Text?.StartsWith("{Winamp.Artist|fallbackvar:Winamp.StreamName|fallback:--}") == true);
            replacementExample.BringIntoView();
            await Task.Delay(50);
            help.UpdateLayout();
            using (var frame = help.CaptureRenderedFrame())
            {
                Assert.NotNull(frame);
                frame.Save(Path.Combine(screenshots, "help-fallback-variable-" + preference + ".png"));
            }
            sections.SelectedIndex = preference == "French" ? 2 : 6;
            await Task.Delay(50);
            help.UpdateLayout();
            using (var frame = help.CaptureRenderedFrame())
            {
                Assert.NotNull(frame);
                frame.Save(Path.Combine(screenshots, "help-details-" + preference + ".png"));
            }
            language.SelectedIndex = preference switch { "English" => 1, "French" => 3, "Turkish" => 4, "Russian" => 5, _ => 2 };
            sections.SelectedIndex = 3;
            await Task.Delay(50);
            help.UpdateLayout();
            var legacyExample = help.GetVisualDescendants().OfType<TextBlock>().Single(text =>
                text.Text?.StartsWith("{WmpLegacy.Artist|fallbackvar:WmpLegacy.StreamName|fallback:--}") == true);
            Assert.Contains(help.GetVisualDescendants().OfType<TextBlock>(), text =>
                text.Text?.StartsWith("{WindowsMedia.Artist|fallbackvar:WindowsMedia.Subtitle|fallback:--}") == true);
            Assert.Contains(help.GetVisualDescendants().OfType<TextBlock>(), text =>
                text.Text?.StartsWith("{Media.Artist|fallback:--}\n{Media.Title|fallback:--}") == true);
            legacyExample.BringIntoView();
            await Task.Delay(50);
            help.UpdateLayout();
            using (var frame = help.CaptureRenderedFrame())
            {
                Assert.NotNull(frame);
                frame.Save(Path.Combine(screenshots, "help-wmp-stream-" + preference + ".png"));
            }
            help.Close();
        }
        finally
        {
            foreach (var dialog in window.OwnedWindows.ToArray()) dialog.Close();
            await window.BeginShutdownAsync();
            window.Close();
            System.Globalization.CultureInfo.CurrentUICulture = originalCulture;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static void Click(Window window, string name) =>
        window.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
}
