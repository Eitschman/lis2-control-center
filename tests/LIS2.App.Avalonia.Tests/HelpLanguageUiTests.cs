using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
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
            sections.SelectedIndex = preference == "French" ? 2 : 6;
            await Task.Delay(50);
            help.UpdateLayout();
            using (var frame = help.CaptureRenderedFrame())
            {
                Assert.NotNull(frame);
                frame.Save(Path.Combine(screenshots, "help-details-" + preference + ".png"));
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
