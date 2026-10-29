using Avalonia.Controls;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using LIS2.App.Avalonia;
using LIS2.Core;
using LIS2.Runtime;

namespace LIS2.App.Avalonia.Tests;

public sealed class SourceQualityUiTests
{
    [AvaloniaTheory]
    [InlineData("German", "de", 1305, 893)]
    [InlineData("Russian", "ru", 950, 600)]
    [InlineData("French", "fr", 1305, 893)]
    [InlineData("Turkish", "tr", 950, 600)]
    [InlineData("English", "en", 1305, 893)]
    public async Task ValidateCorrectPreviewAndSourceFreshness(string language, string code, int width, int height)
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-quality-ui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        await store.SaveAsync(new PortableWorkspace { LanguageMode = language });
        var window = new MainWindow(store, new PortableHaTokenStore(directory, (_, _) => null), false,
            _ => new SerialDisplaySession(new VirtualLis2Transport()),
            fanSensors: new SnapshotFanSensors(() => new Dictionary<string, object?> { ["Hardware.Cpu.Temperature"] = 42.5 })) { Width = width, Height = height };
        try
        {
            window.Show();
            Click(window, "NavButton2"); Click(window, "PageAdd");
            await Task.Delay(100);
            var line = window.FindControl<TextBox>("PageLine1")!;
            line.Text = "{Clock.Time|number:no}";
            window.FindControl<TextBox>("PageLine2")!.Text = "{HA.sensor.offline|fallback:--}";
            window.RefreshTemplateValidation();
            var issues = window.FindControl<StackPanel>("PageLine1Issues")!;
            Assert.True(issues.IsVisible);
            var action = Assert.IsType<Button>(Assert.Single(issues.Children));
            Assert.Contains(SourceQualityText.Get("Decimal places must be 0 to 6", code), Assert.IsType<TextBlock>(action.Content).Text);
            action.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal("number:no", line.SelectedText);
            line.Focus();
            await Task.Delay(150);
            issues.BringIntoView();
            if (width < 1000)
            {
                var scroll = window.FindControl<ScrollViewer>("PagesView")!;
                scroll.Offset += new Vector(0, 300);
            }
            await CaptureAsync(window, "quality-pages-invalid-" + code);
            if (code == "de")
            {
                window.FindControl<ComboBox>("ThemeModeComboBox")!.SelectedIndex = 2;
                await Task.Delay(100);
                window.RefreshTemplateValidation();
                Assert.Equal(global::Avalonia.Styling.ThemeVariant.Dark, window.ActualThemeVariant);
                await CaptureAsync(window, "quality-pages-dark-de");
                window.FindControl<ComboBox>("ThemeModeComboBox")!.SelectedIndex = 1;
            }
            line.Text = "{Clock.Time}";
            window.FindControl<TextBox>("PageLine2")!.Text = "{Clock.Date}";
            window.RefreshTemplateValidation();
            Assert.False(issues.IsVisible);
            Assert.False(window.FindControl<StackPanel>("PageLine2Issues")!.IsVisible);
            await Task.Delay(300);
            Assert.False(string.IsNullOrWhiteSpace(window.FindControl<VfdLine>("PagePreview1")!.Text));
            await CaptureAsync(window, "quality-pages-corrected-" + code);

            Click(window, "NavButton5");
            var now = DateTimeOffset.UtcNow;
            window.ObserveHardwareQuality(new Dictionary<string, object?> { ["Hardware.Cpu.Temperature"] = 42.5 }, now.AddSeconds(-15));
            window.RefreshDataFreshness();
            Assert.Contains(SourceQualityText.Get("Stale", code), window.FindControl<TextBlock>("HardwareFreshness")!.Text);
            await CaptureAsync(window, "quality-hardware-stale-" + code);
            window.ObserveHardwareQuality(new Dictionary<string, object?>(), now);
            Assert.Contains(SourceQualityText.Get("Disconnected", code), window.FindControl<TextBlock>("HardwareFreshness")!.Text);
            window.ObserveHardwareQuality(new Dictionary<string, object?> { ["Hardware.Cpu.Temperature"] = 43 }, now);
            Assert.Contains(SourceQualityText.Get("Current", code), window.FindControl<TextBlock>("HardwareFreshness")!.Text);
            Click(window, "HardwareRefresh");
            await CaptureAsync(window, "quality-hardware-current-" + code);

            Click(window, "NavButton3");
            window.ApplyMprisSnapshot(new MprisSnapshot(["fixture-player"], new Dictionary<string, object?>
            {
                ["Connected"] = true, ["Player"] = "fixture-player", ["Title"] = "Example track", ["Artist"] = "Example artist",
                ["Album"] = "Example album", ["State"] = "Playing", ["Elapsed"] = "00:01:23", ["Duration"] = "00:04:56"
            }));
            window.ObserveMediaQuality("Winamp", new Dictionary<string, object?> { ["Connected"] = true, ["SnapshotAgeSeconds"] = 12d }, now);
            window.RefreshDataFreshness();
            Assert.Contains(SourceQualityText.Get("Stale", code), window.FindControl<TextBlock>("WinampMediaFreshness")!.Text);
            await CaptureAsync(window, "quality-media-" + code);
            Click(window, "NavButton9");
            window.RefreshDataFreshness();
            Assert.Equal(SourceQualityText.Get("No data received", code), window.FindControl<TextBlock>("HaFreshness")!.Text);
            await CaptureAsync(window, "quality-ha-" + code);
        }
        finally { window.Close(); Directory.Delete(directory, true); }
    }

    private static void Click(MainWindow window, string name) =>
        window.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task CaptureAsync(MainWindow window, string name)
    {
        await Task.Delay(80);
        window.UpdateLayout();
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        var output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../TestResults/screenshots"));
        Directory.CreateDirectory(output);
        frame.Save(Path.Combine(output, name + ".png"));
    }
}
