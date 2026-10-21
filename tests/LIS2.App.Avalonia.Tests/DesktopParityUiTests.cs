using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class DesktopParityUiTests
{
    [AvaloniaTheory]
    [InlineData("English", 1)]
    [InlineData("German", 2)]
    [InlineData("French", 3)]
    [InlineData("Turkish", 4)]
    [InlineData("Russian", 5)]
    public async Task ThemesNavigationKeyboardHelpAndNamedGlyphPersistence(string language, int languageIndex)
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-parity-" + Guid.NewGuid().ToString("N"));
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        await store.SaveAsync(new PortableWorkspace { LanguageMode = language });
        var window = new MainWindow(store, new PortableHaTokenStore(directory, "no-keyring"), false,
            _ => throw new InvalidOperationException("No physical hardware in parity tests."));
        try
        {
            window.Show();
            window.Width = 950;
            window.Height = 600;
            var theme = window.FindControl<ComboBox>("ThemeModeComboBox")!;
            foreach (var themeIndex in new[] { 1, 2, 0 })
            {
                theme.SelectedIndex = themeIndex;
                for (var section = 0; section < 10; section++)
                {
                    Click(window, "NavButton" + section);
                    window.UpdateLayout();
                    Assert.True(window.FindControl<ScrollViewer>(DesktopSmoke.Views[section] + "View")!.IsEffectivelyVisible);
                    Assert.True(window.FindControl<Button>("OpenHelp")!.IsEffectivelyVisible);
                    var title = window.FindControl<TextBlock>("SectionTitle")!;
                    var helpButton = window.FindControl<Button>("OpenHelp")!;
                    var titleRight = title.TranslatePoint(new Point(title.Bounds.Width, 0), window)!.Value.X;
                    Assert.True(titleRight <= helpButton.TranslatePoint(default, window)!.Value.X,
                        language + " / " + DesktopSmoke.Views[section] + ": header overlaps help");
                    if (themeIndex == 2 && ((language == "Russian" && section == 6) ||
                        (language == "German" && section == 2) || (language == "French" && section == 9)))
                        await Capture(window, "port06-" + language + "-dark-" + DesktopSmoke.Views[section]);
                }
                if (themeIndex != 0)
                    Assert.Equal(themeIndex == 1 ? ThemeVariant.Light : ThemeVariant.Dark, Application.Current!.ActualThemeVariant);
                if (themeIndex == 2)
                {
                    Click(window, "NavButton7");
                    await Capture(window, "port06-" + language + "-dark-settings");
                }
            }
            Assert.Equal(languageIndex, window.FindControl<ComboBox>("LanguageModeComboBox")!.SelectedIndex);
            Click(window, "NavButton1");
            var name = window.FindControl<TextBox>("GlyphName")!;
            name.Focus();
            Assert.True(name.IsFocused);
            name.Text = "Play icon";
            Click(window, "SaveGlyph");
            await WaitUntil(() => store.Load().CustomGlyphs[0].Name == "Play icon");
            Assert.Contains("{Glyph.Play_icon}", window.FindControl<TextBlock>("GlyphTemplateKey")!.Text);
            window.KeyPressQwerty(PhysicalKey.F1, RawInputModifiers.None);
            window.KeyReleaseQwerty(PhysicalKey.F1, RawInputModifiers.None);
            await WaitUntil(() => window.OwnedWindows.Any(dialog => dialog.Name == "HelpDialog"));
            var help = window.OwnedWindows.Single(dialog => dialog.Name == "HelpDialog");
            Assert.Equal(1, help.GetVisualDescendants().OfType<ListBox>().Single(list => list.Name == "HelpSections").SelectedIndex);
            if (language == "French")
            {
                help.Width = 840;
                help.Height = 600;
                help.GetVisualDescendants().OfType<ListBox>().Single(list => list.Name == "HelpSections").SelectedIndex = 3;
                await Capture(help, "port06-French-media-help");
                var reference = help.GetVisualDescendants().OfType<ScrollViewer>().Single(scroll => scroll.Content is StackPanel);
                reference.Offset = new Vector(0, reference.Extent.Height);
                await Capture(help, "port06-French-media-help-reference");
            }
            help.Close();
            Assert.True(window.IsEnabled);
            Click(window, "NavButton4");
            window.FindControl<TextBox>("EventDuration")!.Text = "0";
            Click(window, "EventShow");
            Assert.Empty(window.FindControl<ListBox>("EventList")!.Items);
            window.FindControl<TextBox>("EventDuration")!.Text = "30";
            Click(window, "EventShow");
            await WaitUntil(() => window.FindControl<ListBox>("EventList")!.Items.Count == 1);
            window.FindControl<ListBox>("EventList")!.SelectedIndex = 0;
            Click(window, "EventCancel");
            await WaitUntil(() => window.FindControl<ListBox>("EventList")!.Items.Count == 0);
            Click(window, "NavButton6");
            var savedFixed = store.Load().Fans.Channels[0].FixedPercent;
            window.FindControl<TextBox>("FanFixed")!.Text = "101";
            Click(window, "FanSave");
            Assert.Equal(savedFixed, store.Load().Fans.Channels[0].FixedPercent);
            window.FindControl<TextBox>("FanFixed")!.Text = "65";
            Click(window, "FanSave");
            await WaitUntil(() => store.Load().Fans.Channels[0].FixedPercent == 65);
            Click(window, "NavButton1");
            if (language == "Russian")
            {
                window.Width = 1305;
                window.Height = 893;
                await Capture(window, "port06-Russian-display-wide");
            }
        }
        finally
        {
            await window.BeginShutdownAsync();
            window.Close();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [AvaloniaFact]
    public async Task SplashRendersBeforeMainWindow()
    {
        var splash = new StartupSplash();
        try { splash.Show(); await Capture(splash, "port06-splash"); }
        finally { splash.Close(); }
    }

    private static void Click(Window window, string name) => window.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task WaitUntil(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < timeout) await Task.Delay(20);
        Assert.True(condition());
    }

    private static async Task Capture(Window window, string name)
    {
        await Task.Delay(60);
        window.UpdateLayout();
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../TestResults/screenshots"));
        Directory.CreateDirectory(directory);
        frame.Save(Path.Combine(directory, name + ".png"));
    }
}
