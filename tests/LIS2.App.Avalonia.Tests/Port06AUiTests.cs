using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Input;
using LIS2.App.Avalonia;
using LIS2.Runtime;

namespace LIS2.App.Avalonia.Tests;

public sealed class Port06AUiTests
{
    [AvaloniaTheory]
    [InlineData("CPU", 2)]
    [InlineData("GPU", 3)]
    [InlineData("Memory", 4)]
    [InlineData("Temperatures", 5)]
    [InlineData("Fans", 6)]
    public async Task BothPresetEntrancesUseSameSensorsFavoritesAndUnits(string preset, int choice)
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-port06a-presets-" + Guid.NewGuid().ToString("N"));
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        await store.SaveAsync(new PortableWorkspace { HardwareSensorPreferences =
            [new() { Key = "Hardware.GPU.Temperature.Core", Alias = "Preferred", IsFavorite = true }] });
        var sensors = new SimulatedFanSensors();
        foreach (var pair in new Dictionary<string, object?>
        {
            ["Hardware.CPU.Load.Total"] = 50d, ["Hardware.CPU.Temperature.Package"] = 45d,
            ["Hardware.GPU.Load.Core"] = 75d, ["Hardware.GPU.Temperature.Core"] = 60d,
            ["Hardware.Memory.Load.Used"] = 40d, ["Hardware.Memory.Data.Used"] = 16d,
            ["Hardware.CPU.Fan.Fan1"] = 1200d, ["Hardware.CPU.Fan.Fan2"] = 1300d,
            ["Hardware.CPU.Temperature.Package.Unit"] = "°C", ["Hardware.GPU.Temperature.Core.Unit"] = "°C"
        }) sensors.Set(pair.Key, pair.Value);
        var window = new MainWindow(store, new PortableHaTokenStore(directory, "no-keyring"), false,
            _ => throw new InvalidOperationException(), fanSensors: sensors);
        try
        {
            window.Show();
            Click(window, "NavButton5");
            Click(window, "HardwareRefresh");
            Click(window, "NavButton2");
            var picker = window.FindControl<ComboBox>("PagePresetPicker")!;
            Assert.Equal(OperatingSystem.IsLinux(), ((ComboBoxItem)picker.Items[11]!).IsEnabled);
            Assert.Equal(OperatingSystem.IsWindows(), ((ComboBoxItem)picker.Items[7]!).IsEnabled);
            picker.SelectedIndex = choice;
            Click(window, "PageCreatePreset");
            await Wait(() => store.Load().Pages.Count == 1);
            var page = store.Load().Pages.Single();
            Click(window, "NavButton5");
            Click(window, "HardwarePreset" + preset);
            await Wait(() => store.Load().Pages.Count == 2);
            var other = store.Load().Pages.Last();
            Assert.Equal(page.Line1Template, other.Line1Template);
            Assert.Equal(page.Line2Template, other.Line2Template);
            if (preset == "Temperatures") Assert.Contains("GPU", page.Line1Template);
            Click(window, "NavButton6");
            var choices = window.FindControl<ComboBox>("FanSensorChoices")!;
            choices.SelectedIndex = 0;
            Assert.StartsWith("Hardware.", window.FindControl<TextBox>("FanSensorKey")!.Text);
        }
        finally
        {
            await window.BeginShutdownAsync();
            window.Close();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GlyphSynchronizationRequiresOptionAndSaveFailuresKeepDrafts(bool synchronize)
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-port06a-save-" + Guid.NewGuid().ToString("N"));
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        await store.SaveAsync(new PortableWorkspace { LanguageMode = "English", ProgramCustomGlyphsOnConnect = synchronize });
        var window = new MainWindow(store, new PortableHaTokenStore(directory, "no-keyring"), false,
            _ => throw new InvalidOperationException("No physical hardware."));
        try
        {
            window.Show();
            await Task.Delay(100);
            Click(window, "NavButton1");
            Click(window, "LoadExampleGlyphs");
            await Wait(() => window.FindControl<TextBlock>("GlyphSaveStatus")!.Text == (synchronize ? "Saved and sent to display." : "Saved"));
            var report = window.BuildDiagnosticsReport();
            Assert.Equal(synchronize, report.Contains("Custom glyph slot", StringComparison.OrdinalIgnoreCase));
            Directory.CreateDirectory(store.FilePath + ".tmp");
            var bitmap = window.FindControl<TextBox>("GlyphBitmapEditor")!;
            bitmap.Text = string.Join("\n", Enumerable.Repeat("10101", 8));
            Click(window, "SaveGlyph");
            await Wait(() => window.FindControl<TextBlock>("GlyphSaveStatus")!.Text!.StartsWith("Save failed:"));
            Assert.Equal(string.Join("\n", Enumerable.Repeat("10101", 8)), bitmap.Text);
            Click(window, "NavButton2");
            Click(window, "PageAdd");
            window.FindControl<TextBox>("PageName")!.Text = "Unsaved draft";
            Click(window, "PageSave");
            await Wait(() => window.FindControl<TextBlock>("PageSaveStatus")!.Text!.StartsWith("Save failed:"));
            Assert.Equal("Unsaved draft", window.FindControl<TextBox>("PageName")!.Text);
            Assert.DoesNotContain(store.Load().Pages, page => page.Name == "Unsaved draft");
            Directory.Delete(store.FilePath + ".tmp");
            Click(window, "PageSave");
            await Wait(() => window.FindControl<TextBlock>("PageSaveStatus")!.Text == "Saved");
            Assert.Contains(store.Load().Pages, page => page.Name == "Unsaved draft");
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
    public async Task EditorsRoundTripAndMediaLossClearsGraphs(string preference, string language)
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-port06a-ui-" + Guid.NewGuid().ToString("N"));
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        await store.SaveAsync(new PortableWorkspace { LanguageMode = preference });
        var window = new MainWindow(store, new PortableHaTokenStore(directory, "no-keyring"), false,
            _ => throw new InvalidOperationException("No physical hardware in editor tests."));
        try
        {
            window.Show();
            window.Width = 950;
            window.Height = 600;
            Click(window, "NavButton2");
            Click(window, "PageAdd");
            var raw = window.FindControl<TextBox>("PageVisibility")!;
            raw.Text = "Hardware.CPU.Temperature>=42.125";
            await Wait(() => window.FindControl<TextBox>("PageConditionValue")!.Text == "42.125");
            Assert.Equal("Hardware.CPU.Temperature", window.FindControl<TextBox>("PageConditionKey")!.Text);
            Assert.Equal(3, window.FindControl<ComboBox>("PageConditionOperator")!.SelectedIndex);
            Click(window, "PageConditionBrowse");
            await Wait(() => window.OwnedWindows.Count > 0);
            var browser = window.OwnedWindows.Single();
            browser.GetLogicalDescendants().OfType<TextBox>().First().Text = "Clock.Date";
            var results = browser.GetLogicalDescendants().OfType<ListBox>().Single();
            await Wait(() => results.ItemCount > 0);
            results.SelectedIndex = 0;
            browser.GetLogicalDescendants().OfType<Button>().Single(button => Equals(button.Content,
                UiTranslations.Translate("Use selected variable", language))).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Wait(() => window.OwnedWindows.Count == 0);
            Assert.Equal("Clock.Date", window.FindControl<TextBox>("PageConditionKey")!.Text);
            raw.Text = "broken condition";
            await Wait(() => window.FindControl<TextBox>("PageConditionKey")!.Text == "");
            Assert.Equal("broken condition", raw.Text);
            raw.Text = "";
            window.FindControl<TextBox>("PageConditionKey")!.Text = "Clock.Hour";
            window.FindControl<TextBox>("PageConditionValue")!.Text = "5";
            Click(window, "PageApplyCondition");
            Assert.Equal("Clock.Hour=5", raw.Text);
            Click(window, "PageSave");
            await Wait(() => window.FindControl<TextBlock>("PageSaveStatus")!.Text == UiTranslations.Translate("Saved", language));
            Assert.Contains(store.Load().Pages, page => page.VisibilityExpression == "Clock.Hour=5");
            await Capture(window, "pages-" + language);
            window.FindControl<Button>("PageApplyCondition")!.BringIntoView();
            await Capture(window, "conditions-" + language);

            Click(window, "NavButton6");
            Assert.False(window.FindControl<Expander>("FanTestTools")!.IsExpanded);
            Assert.Equal(UiTranslations.Translate("Test tools", language), window.FindControl<Expander>("FanTestTools")!.Header);
            window.FindControl<TextBox>("FanSensorKey")!.Text = "Hardware.Missing.Manual";
            Click(window, "FanSave");
            await Wait(() => store.Load().Fans.Channels[0].SensorKey == "Hardware.Missing.Manual");
            window.FindControl<ComboBox>("FanChannelPicker")!.SelectedIndex = 1;
            window.FindControl<ComboBox>("FanChannelPicker")!.SelectedIndex = 0;
            Assert.Equal("Hardware.Missing.Manual", window.FindControl<TextBox>("FanSensorKey")!.Text);
            Assert.Contains(window.FindControl<ComboBox>("FanSensorChoices")!.Items, item => item!.ToString()!.Contains("Hardware.Missing.Manual"));
            var curve = window.FindControl<TextBox>("FanCurve")!;
            curve.Text = "20.123456789=30\n60=90";
            await Wait(() => window.FindControl<ListBox>("FanCurvePointsList")!.ItemCount == 2);
            window.FindControl<Expander>("FanCurvePointsExpander")!.IsExpanded = true;
            window.FindControl<ListBox>("FanCurvePointsList")!.SelectedIndex = 0;
            Assert.Equal("20.123456789", window.FindControl<TextBox>("FanPointTemperature")!.Text);
            window.FindControl<TextBox>("FanPointOutput")!.Text = "35";
            Click(window, "FanPointApply");
            Assert.Contains("20.123456789=35", curve.Text);
            await Capture(window, "fans-" + language);
            window.FindControl<Button>("FanPointApply")!.BringIntoView();
            await Capture(window, "fan-points-" + language);

            Click(window, "NavButton1");
            var bitmap = window.FindControl<TextBox>("GlyphBitmapEditor")!;
            bitmap.Text = string.Join("\n", Enumerable.Repeat("00000", 8));
            await Task.Delay(30);
            var pixel = window.FindControl<UniformGrid>("GlyphPixelGrid")!.Children.OfType<ToggleButton>().First();
            pixel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.StartsWith("10", bitmap.Text);
            Assert.True(pixel.Focus());
            window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
            window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
            Assert.StartsWith("00", bitmap.Text);
            pixel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Click(window, "SaveGlyph");
            await Wait(() => window.FindControl<TextBlock>("GlyphSaveStatus")!.Text == UiTranslations.Translate("Saved", language));
            await Capture(window, "glyphs-" + language);
            window.FindControl<UniformGrid>("GlyphPixelGrid")!.BringIntoView();
            await Capture(window, "glyph-pixels-" + language);

            Click(window, "NavButton3");
            window.RefreshMediaSource("Winamp", new Dictionary<string, object?>
            {
                ["Connected"] = true, ["Title"] = "Connected", ["VuLeft"] = 255, ["VuRight"] = 128,
                ["SpectrumRaw"] = Enumerable.Range(0, 20).Select(index => index % 16).ToArray()
            });
            Assert.Equal(255, window.FindControl<ProgressBar>("WinampVuLeft")!.Value);
            Assert.Equal("Connected", window.FindControl<TextBlock>("WinampMediaTitle")!.Text);
            await Capture(window, "media-" + language);
            window.FindControl<ScrollViewer>("MediaView")!.ScrollToEnd();
            await Capture(window, "media-levels-" + language);
            window.RefreshMediaSource("Winamp", new Dictionary<string, object?> { ["Connected"] = false, ["Title"] = "stale" });
            Assert.Equal(0, window.FindControl<ProgressBar>("WinampVuLeft")!.Value);
            Assert.Equal(UiTranslations.Translate("Nothing playing", language), window.FindControl<TextBlock>("WinampMediaTitle")!.Text);

            Click(window, "NavButton8");
            var report = window.BuildDiagnosticsReport();
            Assert.Contains("UTC:", report);
            Assert.Contains("Version:", report);
            Assert.Contains("fail-safe=", report);
            await Capture(window, "diagnostics-" + language);
        }
        finally
        {
            await window.BeginShutdownAsync();
            window.Close();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static void Click(Window window, string name) => window.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static async Task Wait(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Editor update timed out.");
            await Task.Delay(20);
        }
    }
    private static async Task Capture(Window window, string name)
    {
        await Task.Delay(60);
        window.UpdateLayout();
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../TestResults/screenshots"));
        Directory.CreateDirectory(directory);
        frame.Save(Path.Combine(directory, "port06a-" + name + ".png"));
    }
}
