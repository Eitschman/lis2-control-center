using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class MediaUiTests
{
    [AvaloniaTheory]
    [InlineData("German", "de")]
    [InlineData("French", "fr")]
    [InlineData("Russian", "ru")]
    public async Task PlayerSelectionPersistsAndDisconnectedMetadataIsCleared(string preference, string language)
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-media-" + Guid.NewGuid().ToString("N"));
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        await store.SaveAsync(new PortableWorkspace { LanguageMode = preference, LinuxMediaPlayer = "plexamp" });
        var window = new MainWindow(store, new PortableHaTokenStore(directory, "lis2-test-no-keyring"), false,
            _ => throw new InvalidOperationException("No serial hardware in media tests."));
        try
        {
            window.Show();
            Click(window, "NavButton3");
            Assert.Null(window.FindControl<Border>("WmpLegacyMediaCard"));
            Assert.Equal(OperatingSystem.IsWindows(), window.FindControl<Border>("WindowsMediaCard")!.IsVisible);
            // Die Linux-Karte wird auch auf Windows mit denselben tatsächlichen Controls geprüft.
            window.FindControl<Border>("WindowsMediaCard")!.IsVisible = false;
            window.FindControl<Border>("WinampMediaCard")!.IsVisible = false;
            window.FindControl<Border>("LinuxMediaCard")!.IsVisible = true;
            var picker = window.FindControl<ComboBox>("LinuxMediaPlayer")!;
            Assert.Equal("plexamp", picker.SelectedItem);
            window.ApplyMprisSnapshot(new(["vlc", "plexamp"], LinuxMprisSource.Parse(LinuxMprisSourceTests.Metadata)));
            Assert.Equal("plexamp", picker.SelectedItem);
            Assert.Equal("A | B & <C>\nTitel", window.FindControl<TextBlock>("LinuxMediaTitle")!.Text);
            Assert.Equal("Connected", window.FindControl<TextBlock>("LinuxMediaArtist")!.Text);
            Assert.Equal("00:01:05 / 25:01:01", window.FindControl<TextBlock>("LinuxMediaDuration")!.Text);
            await Capture(window, "port04-media-" + language + "-wide");
            window.Width = 950;
            window.Height = 600;
            await Capture(window, "port04-media-" + language + "-small");
            picker.SelectedItem = "vlc";
            await WaitAsync(() => store.Load().LinuxMediaPlayer == "vlc");
            Assert.Equal(UiTranslations.Translate("Nothing playing", language), window.FindControl<TextBlock>("LinuxMediaTitle")!.Text);
            window.ApplyMprisSnapshot(new(["plexamp"], LinuxMprisSource.Map("", "", "")));
            Assert.Equal("vlc", picker.SelectedItem);
            Assert.Equal(UiTranslations.Translate("Disconnected", language), window.FindControl<TextBlock>("LinuxMediaStatus")!.Text);
            await Capture(window, "port04-media-" + language + "-disconnected");
            window.FindControl<ComboBox>("LanguageModeComboBox")!.SelectedIndex = 1;
            Assert.Equal("vlc", picker.SelectedItem);
            Assert.Equal("Automatic player", picker.Items[0]);
            picker.SelectedIndex = 0;
            await WaitAsync(() => store.Load().LinuxMediaPlayer is null);
            Click(window, "LinuxMediaPage");
            await WaitAsync(() => store.Load().Pages.Any(page => page.Line1Template == "{Media.Title}"));
            Assert.Contains(store.Load().Pages, page => page.Line2Template == "{Media.Artist}");
        }
        finally
        {
            await window.BeginShutdownAsync();
            window.Close();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static void Click(Window window, string name) => window.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task WaitAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Media workspace update timed out.");
            await Task.Delay(20);
        }
    }

    private static async Task Capture(Window window, string name)
    {
        await Task.Delay(60);
        window.UpdateLayout();
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Assert.Equal((int)window.Width, frame.PixelSize.Width);
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../TestResults/screenshots"));
        Directory.CreateDirectory(directory);
        frame.Save(Path.Combine(directory, name + ".png"));
    }
}
