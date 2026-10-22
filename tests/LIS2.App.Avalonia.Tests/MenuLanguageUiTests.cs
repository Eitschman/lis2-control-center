using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class MenuLanguageUiTests
{
    [AvaloniaTheory]
    [InlineData("German", "de")]
    [InlineData("English", "en")]
    [InlineData("French", "fr")]
    [InlineData("Turkish", "tr")]
    [InlineData("Russian", "ru")]
    [InlineData("System", "de")]
    public async Task AllTenViewsAndStatusesFollowLanguageWithoutChangingData(string preference, string language)
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture;
        if (preference == "System") System.Globalization.CultureInfo.CurrentUICulture = new("de-DE");
        var directory = Path.Combine(Path.GetTempPath(), "lis2-menu-all-" + Guid.NewGuid().ToString("N"));
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        await store.SaveAsync(new PortableWorkspace { LanguageMode = preference });
        var window = new MainWindow(store, new PortableHaTokenStore(directory, "lis2-test-no-keyring"), false,
            _ => throw new InvalidOperationException("No serial hardware in language tests."));
        try
        {
            window.Show();
            var markers = new[] { "System Status", "Display controls", "Pages", "Nothing playing",
                "Active / queued events", "Hardware monitoring", "Fan channel configuration", "Appearance",
                "Diagnostics Snapshot", "Home Assistant entities" };
            for (var index = 0; index < markers.Length; index++)
            {
                Click(window, "NavButton" + index);
                window.UpdateLayout();
                var view = window.FindControl<ScrollViewer>(DesktopSmoke.Views[index] + "View")!;
                Assert.True(view.IsEffectivelyVisible);
                var expected = UiTranslations.Translate(markers[index], language);
                Assert.True(view.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == expected),
                    preference + " / " + DesktopSmoke.Views[index] + " / " + expected + ": " +
                    string.Join(" | ", view.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text)));
                if (preference == "German")
                {
                    await Capture(window, "menu-de-" + DesktopSmoke.Views[index] + "-wide");
                    window.Width = 950;
                    window.Height = 600;
                    await Capture(window, "menu-de-" + DesktopSmoke.Views[index] + "-small");
                    view.Offset = new global::Avalonia.Vector(0, view.Extent.Height);
                    await Capture(window, "menu-de-" + DesktopSmoke.Views[index] + "-bottom");
                    view.Offset = default;
                    window.Width = 1305;
                    window.Height = 893;
                    await Task.Delay(50);
                }
                if (preference is "French" or "Russian")
                {
                    window.Width = 950;
                    window.Height = 600;
                    await Capture(window, "menu-" + language + "-" + DesktopSmoke.Views[index] + "-small");
                    view.Offset = new global::Avalonia.Vector(0, view.Extent.Height);
                    await Capture(window, "menu-" + language + "-" + DesktopSmoke.Views[index] + "-bottom");
                    view.Offset = default;
                    window.Width = 1305;
                    window.Height = 893;
                    await Task.Delay(50);
                }
            }
            Click(window, "OpenAbout");
            var localizedAbout = Assert.Single(window.OwnedWindows);
            localizedAbout.UpdateLayout();
            Assert.Contains(localizedAbout.GetVisualDescendants().OfType<TextBlock>(), text =>
                text.Text == UiTranslations.Translate("Operating system", language));
            var aboutTexts = localizedAbout.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ToArray();
            Assert.Contains("MIT", aboutTexts);
            Assert.Contains("11.3.22", aboutTexts);
            Assert.Contains(UiTranslations.Translate("License", language), aboutTexts);
            var licenses = localizedAbout.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "AboutLicenses");
            Assert.Equal(UiTranslations.Translate("Licenses", language), licenses.Content);
            Assert.False(Directory.Exists(Path.Combine(AppContext.BaseDirectory, "licenses")));
            licenses.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(UiTranslations.Translate("License files are not installed.", language),
                localizedAbout.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Name == "AboutLicenseStatus").Text);
            if (preference is "German" or "French" or "Russian") await Capture(localizedAbout, "menu-" + language + "-About");
            localizedAbout.Close();
            Click(window, "NavButton4");
            window.FindControl<TextBox>("EventLine1")!.Text = "Connected";
            window.FindControl<TextBox>("EventDuration")!.Text = "0";
            Click(window, "EventShow");
            Assert.Equal(UiTranslations.Translate("Duration must be between 1 and 3600 seconds.", language),
                window.FindControl<TextBlock>("EventStatus")!.Text);
            window.FindControl<ComboBox>("LanguageModeComboBox")!.SelectedIndex = 1;
            window.FindControl<TextBlock>("WindowsMediaTitle")!.Text = "Connected";
            window.FindControl<TextBlock>("PreviewLine1")!.Text = "Playing";
            Assert.Equal("Duration must be between 1 and 3600 seconds.", window.FindControl<TextBlock>("EventStatus")!.Text);
            Assert.Equal("Connected", window.FindControl<TextBox>("EventLine1")!.Text);
            window.FindControl<ComboBox>("LanguageModeComboBox")!.SelectedIndex = 2;
            Assert.Equal("Connected", window.FindControl<TextBlock>("WindowsMediaTitle")!.Text);
            Assert.Equal("Playing", window.FindControl<TextBlock>("PreviewLine1")!.Text);
            Click(window, "NavButton6");
            window.FindControl<ComboBox>("FanModePicker")!.SelectedIndex = 1;
            Click(window, "FanSave");
            var timeout = DateTime.UtcNow.AddSeconds(5);
            // Die Datei kann schon sichtbar sein, bevor die asynchrone UI-Fortsetzung den Status setzt.
            var savedStatus = UiTranslations.Translate("Fan settings saved. Physical fan output requires separate authorization.", "de");
            while ((store.Load().Fans.Channels[0].Mode != "Curve" ||
                window.FindControl<TextBlock>("FanPreviewStatus")!.Text != savedStatus) && DateTime.UtcNow < timeout)
                await Task.Delay(20);
            Assert.Equal("Curve", store.Load().Fans.Channels[0].Mode);
            Assert.Equal(UiTranslations.Translate("Fan settings saved. Physical fan output requires separate authorization.", "de"),
                window.FindControl<TextBlock>("FanPreviewStatus")!.Text);
            Assert.Contains("Plattform:", window.BuildDiagnosticsReport());
            Assert.Equal("Alle Bereiche", window.FindControl<ComboBox>("HaDomainFilter")!.Items[0]);
        }
        finally
        {
            await window.BeginShutdownAsync();
            window.Close();
            System.Globalization.CultureInfo.CurrentUICulture = culture;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static void Click(Window window, string name) =>
        window.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task Capture(Window window, string name)
    {
        var expectedWidth = name.EndsWith("-small", StringComparison.Ordinal) || name.EndsWith("-bottom", StringComparison.Ordinal) ? 950 : (int)window.Width;
        var expectedHeight = expectedWidth == 950 ? 600 : (int)window.Height;
        await Task.Delay(50);
        window.UpdateLayout();
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Assert.Equal(expectedWidth, frame.PixelSize.Width);
        Assert.Equal(expectedHeight, frame.PixelSize.Height);
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../TestResults/screenshots"));
        Directory.CreateDirectory(directory);
        frame.Save(Path.Combine(directory, name + ".png"));
    }

    [AvaloniaFact]
    public async Task GermanFanPageTranslatesAvaloniaSpecificLabels()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-menu-language-" + Guid.NewGuid().ToString("N"));
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        await store.SaveAsync(new PortableWorkspace { LanguageMode = "German" });
        var window = new MainWindow(store, new PortableHaTokenStore(directory, "lis2-test-no-keyring"), false,
            _ => throw new InvalidOperationException("No serial hardware in language tests."));
        try
        {
            window.Show();
            Assert.Equal(2, window.FindControl<ComboBox>("LanguageModeComboBox")!.SelectedIndex);
            window.FindControl<Button>("NavButton6")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.UpdateLayout();
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == "Lüfterkanal konfigurieren");
        }
        finally
        {
            await window.BeginShutdownAsync();
            window.Close();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
