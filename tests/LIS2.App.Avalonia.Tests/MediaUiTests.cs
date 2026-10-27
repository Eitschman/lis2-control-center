using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using LIS2.App.Avalonia;
using LIS2.WmpLegacy;
using System.Reflection;
using Avalonia.Threading;

namespace LIS2.App.Avalonia.Tests;

public sealed class MediaUiTests
{
    [AvaloniaFact]
    public async Task LegacyAudioChangesReachActualControlsWithinFourHundredMilliseconds()
    {
        if (!OperatingSystem.IsWindows()) return;
        var directory = Path.Combine(Path.GetTempPath(), "lis2-media-cadence-" + Guid.NewGuid().ToString("N"));
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        var window = new MainWindow(store, new PortableHaTokenStore(directory, "lis2-test-no-keyring"), false,
            _ => throw new InvalidOperationException("No serial hardware in media cadence tests."));
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var timer = (DispatcherTimer)typeof(MainWindow).GetField("_mediaTimer", flags)!.GetValue(window)!;
        var source = (WmpLegacyDataSource)typeof(MainWindow).GetField("_mediaLegacy", flags)!.GetValue(window)!;
        var apply = typeof(WmpLegacyDataSource).GetMethod("ApplyVisualization", flags)!;
        try
        {
            window.Show();
            Click(window, "NavButton3");
            Assert.True(source.ProcessAudioEnabled);
            timer.Start();
            foreach (var level in new[] { 32, 96, 160, 224 })
            {
                apply.Invoke(source, [new WmpLegacyVisualizationMessage
                {
                    VuLeft = level, VuRight = 255 - level,
                    Spectrum = Enumerable.Repeat(level, 20).ToArray()
                }]);
                var elapsed = System.Diagnostics.Stopwatch.StartNew();
                while (window.FindControl<ProgressBar>("WmpLegacyVuLeft")!.Value != level)
                {
                    Assert.True(elapsed.Elapsed < TimeSpan.FromMilliseconds(400),
                        "Legacy audio controls retained the old sample for 400 ms.");
                    await Task.Delay(10);
                }
                Assert.Equal(255 - level, window.FindControl<ProgressBar>("WmpLegacyVuRight")!.Value);
                Assert.All(window.FindControl<Grid>("WmpLegacySpectrumBars")!.Children.Cast<ProgressBar>(),
                    bar => Assert.Equal(level, bar.Value));
            }
        }
        finally
        {
            timer.Stop();
            await window.BeginShutdownAsync();
            window.Close();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [AvaloniaFact]
    public async Task LegacySpectrumPresetRequiresGlyphReplacementAndHonorsCancel()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-legacy-preset-" + Guid.NewGuid().ToString("N"));
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        await store.SaveAsync(new PortableWorkspace { LanguageMode = "English" });
        var window = new MainWindow(store, new PortableHaTokenStore(directory, "no-keyring"), false,
            _ => throw new InvalidOperationException("No serial hardware in preset tests."));
        try
        {
            window.Show();
            Click(window, "NavButton2");
            var picker = window.FindControl<ComboBox>("PagePresetPicker")!;
            Assert.Equal(OperatingSystem.IsWindows(), ((ComboBoxItem)picker.Items[14]!).IsEnabled);
            ((ComboBoxItem)picker.Items[14]!).IsEnabled = true;
            picker.SelectedIndex = 14;
            Click(window, "PageCreatePreset");
            await WaitAsync(() => window.OwnedWindows.Any());
            var dialog = window.OwnedWindows.Single();
            dialog.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "Cancel"))
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await WaitAsync(() => !window.OwnedWindows.Any());
            Assert.Empty(store.Load().Pages);
            Click(window, "PageCreatePreset");
            await WaitAsync(() => window.OwnedWindows.Any());
            dialog = window.OwnedWindows.Single();
            dialog.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "Replace glyphs"))
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await WaitAsync(() => store.Load().Pages.Count == 1);
            Assert.Equal("{WmpLegacy.SpectrumGlyphs}", store.Load().Pages.Single().Line2Template);
            Click(window, "PageCreatePreset");
            await WaitAsync(() => store.Load().Pages.Count == 2);
            Assert.Empty(window.OwnedWindows);
        }
        finally
        {
            await window.BeginShutdownAsync();
            window.Close();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [AvaloniaTheory]
    [InlineData("English", "en")]
    [InlineData("German", "de")]
    [InlineData("French", "fr")]
    [InlineData("Turkish", "tr")]
    [InlineData("Russian", "ru")]
    public async Task LegacyMetadataAudioAndPageCreationUseIndependentConnectionStates(string preference, string language)
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-legacy-ui-" + Guid.NewGuid().ToString("N"));
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        await store.SaveAsync(new PortableWorkspace { LanguageMode = preference });
        var window = new MainWindow(store, new PortableHaTokenStore(directory, "lis2-test-no-keyring"), false,
            _ => throw new InvalidOperationException("No serial hardware in media tests."));
        try
        {
            window.Show();
            Click(window, "NavButton3");
            foreach (var name in new[] { "LinuxMediaCard", "WindowsMediaCard", "WinampMediaCard" })
                window.FindControl<Border>(name)!.IsVisible = false;
            window.FindControl<Border>("WmpLegacyMediaCard")!.IsVisible = true;
            var values = new Dictionary<string, object?>
            {
                ["Connected"] = true, ["VisualizationConnected"] = true,
                ["Title"] = "Connected", ["Artist"] = "Björk & Massive Attack", ["Album"] = "Mezzanine",
                ["State"] = "Playing", ["TrackNumber"] = 3, ["PlaylistCount"] = 12,
                ["Elapsed"] = "01:05", ["Duration"] = "04:12", ["VuLeft"] = 192, ["VuRight"] = 128,
                ["Vu"] = "======    ====", ["Spectrum"] = ".:-=+*#",
                ["SpectrumRaw"] = Enumerable.Range(0, 20).Select(index => index * 13).ToArray()
            };
            window.RefreshMediaSource("WmpLegacy", values);
            Assert.Equal("Connected", window.FindControl<TextBlock>("WmpLegacyMediaTitle")!.Text);
            Assert.Equal(UiTranslations.Translate("Connected", language), window.FindControl<TextBlock>("WmpLegacyMediaStatus")!.Text);
            Assert.Equal(UiTranslations.Translate("Playing", language), window.FindControl<TextBlock>("WmpLegacyMediaPlayback")!.Text);
            Assert.Equal("3 / 12", window.FindControl<TextBlock>("WmpLegacyMediaPlaylist")!.Text);
            Assert.Equal(192, window.FindControl<ProgressBar>("WmpLegacyVuLeft")!.Value);
            Assert.Equal(20, window.FindControl<Grid>("WmpLegacySpectrumBars")!.Children.Count);
            Assert.Equal(UiTranslations.Translate("Direct player audio", language),
                window.FindControl<TextBlock>("WmpLegacyVisualizationConnection")!.Text);
            values["AudioCaptureError"] = "Direct audio capture requires Windows build 20348 or later.";
            window.RefreshMediaSource("WmpLegacy", values);
            Assert.Contains(UiTranslations.Translate("Direct audio capture requires Windows build 20348 or later.", language),
                window.FindControl<TextBlock>("WmpLegacyVisualizationConnection")!.Text);
            values.Remove("AudioCaptureError");
            window.RefreshMediaSource("WmpLegacy", values);
            await Capture(window, "legacy-media-" + language + "-wide");
            window.Width = 950;
            window.Height = 600;
            await Capture(window, "legacy-media-" + language + "-small");
            values["Connected"] = false;
            window.RefreshMediaSource("WmpLegacy", values);
            Assert.Equal(UiTranslations.Translate("Nothing playing", language), window.FindControl<TextBlock>("WmpLegacyMediaTitle")!.Text);
            Assert.Equal(192, window.FindControl<ProgressBar>("WmpLegacyVuLeft")!.Value);
            values["Connected"] = true;
            values["VisualizationConnected"] = false;
            window.RefreshMediaSource("WmpLegacy", values);
            Assert.Equal("Connected", window.FindControl<TextBlock>("WmpLegacyMediaTitle")!.Text);
            Assert.Equal(0, window.FindControl<ProgressBar>("WmpLegacyVuLeft")!.Value);
            Assert.Equal(UiTranslations.Translate("Disconnected", language), window.FindControl<TextBlock>("WmpLegacyVisualizationStatus")!.Text);
            values["Connected"] = false;
            window.RefreshMediaSource("WmpLegacy", values);
            Assert.Equal(UiTranslations.Translate("Nothing playing", language), window.FindControl<TextBlock>("WmpLegacyMediaTitle")!.Text);
            Assert.Equal("", window.FindControl<TextBlock>("WmpLegacyMediaArtist")!.Text);
            Click(window, "WmpLegacyMediaPage");
            await WaitAsync(() => store.Load().Pages.Any(page => page.Line1Template == "{WmpLegacy.Title}"));
            Assert.Contains(store.Load().Pages, page => page.Line2Template == "{WmpLegacy.Artist}");
            await window.StartMediaProviderSafeAsync("WmpLegacy", () => throw new IOException("Legacy pipe unavailable"));
            Assert.Contains("Legacy pipe unavailable", window.FindControl<TextBlock>("WmpLegacyMediaStatus")!.Text);
        }
        finally
        {
            await window.BeginShutdownAsync();
            window.Close();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

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
            Assert.Equal(OperatingSystem.IsWindows(), window.FindControl<Border>("WmpLegacyMediaCard")!.IsVisible);
            Assert.Equal(OperatingSystem.IsWindows(), window.FindControl<Border>("WindowsMediaCard")!.IsVisible);
            // Die Linux-Karte wird auch auf Windows mit denselben tatsächlichen Controls geprüft.
            window.FindControl<Border>("WindowsMediaCard")!.IsVisible = false;
            window.FindControl<Border>("WinampMediaCard")!.IsVisible = false;
            window.FindControl<Border>("WmpLegacyMediaCard")!.IsVisible = false;
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
