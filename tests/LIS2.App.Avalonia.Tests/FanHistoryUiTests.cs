using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using LIS2.App.Avalonia;
using LIS2.Core;

namespace LIS2.App.Avalonia.Tests;

public sealed class FanHistoryUiTests
{
    [AvaloniaTheory]
    [InlineData("German", "de", 1305, 893)]
    [InlineData("English", "en", 1305, 893)]
    [InlineData("French", "fr", 1305, 893)]
    [InlineData("Turkish", "tr", 950, 600)]
    [InlineData("Russian", "ru", 950, 600)]
    public async Task DisplaysHistoryGapsAndIndependentChannels(string language, string code, int width, int height)
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-history-ui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        var workspace = new PortableWorkspace { LanguageMode = language };
        workspace.Fans.Channels[0].Mode = "Curve";
        workspace.Fans.Channels[0].SensorKey = "Hardware.Cpu.Temperature";
        workspace.Dashboard.FanSpeedKeys = ["Hardware.Fan.Speed", "", "", ""];
        await store.SaveAsync(workspace);
        var window = new MainWindow(store, new PortableHaTokenStore(directory, (_, _) => null), false,
            _ => new SerialDisplaySession(new VirtualLis2Transport())) { Width = width, Height = height };
        try
        {
            window.Show();
            await Task.Delay(150);
            window.FindControl<Button>("NavButton6")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var now = DateTimeOffset.UtcNow;
            for (var index = 0; index <= 150; index++)
                window.RecordFanHistory(index is >= 50 and <= 55 ? new Dictionary<string, object?>() : new Dictionary<string, object?>
                {
                    ["Hardware.Cpu.Temperature"] = 45 + Math.Sin(index / 12.0) * 8,
                    ["Hardware.Fan.Speed"] = 1000 + Math.Sin(index / 12.0) * 300
                }, now.AddSeconds((index - 150) * 2));
            Assert.Contains("RPM", window.FindControl<TextBlock>("HistoryRpm")!.Text);
            Assert.Contains("%", window.FindControl<TextBlock>("HistoryTarget")!.Text);
            Assert.Equal(FanHistoryText.Get("5 min", code), window.FindControl<ComboBox>("HistoryWindow")!.SelectedItem);
            window.FindControl<Control>("HistoryChannel")!.BringIntoView();
            if (width < 1000)
                window.FindControl<ScrollViewer>("FanControlView")!.Offset += new Vector(0, 230);
            await Capture(window, "fan-history-" + code);
            if (code == "de")
            {
                window.FindControl<ComboBox>("ThemeModeComboBox")!.SelectedIndex = 2;
                await Capture(window, "fan-history-dark-de");
            }
            window.FindControl<ComboBox>("HistoryChannel")!.SelectedIndex = 1;
            Assert.Equal(FanHistoryText.Get("No speed sensor assigned", code), window.FindControl<TextBlock>("HistoryRpm")!.Text);
            window.FindControl<ComboBox>("HistoryWindow")!.SelectedIndex = 2;
            window.RecordFanHistory(new Dictionary<string, object?>(), now.AddSeconds(2));
            window.FindControl<ComboBox>("HistoryChannel")!.SelectedIndex = 0;
            Assert.Equal(UiTranslations.Translate("Unavailable", code), window.FindControl<TextBlock>("HistoryRpm")!.Text);
            Assert.Contains("%", window.FindControl<TextBlock>("HistoryTarget")!.Text);
            foreach (var source in FanHistoryText.Sources)
                Assert.True(UiTranslations.HasTranslation(source, code));
            Assert.Contains(HelpReferenceCatalog.GetSection(6, [], [], code), block => block.Body == FanHistoryText.Get(FanHistoryText.Help, code));
            window.FindControl<Button>("OpenHelp")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var help = window.OwnedWindows.Single(dialog => dialog.Name == "HelpDialog");
            help.Width = 840; help.Height = 600;
            help.GetVisualDescendants().OfType<ListBox>().Single().SelectedIndex = 6;
            var paragraph = help.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Text == FanHistoryText.Get(FanHistoryText.Help, code));
            Assert.Equal(global::Avalonia.Media.TextWrapping.Wrap, paragraph.TextWrapping);
            paragraph.BringIntoView();
            await Capture(help, "fan-history-help-" + code);
            help.Close();
        }
        finally { window.Close(); Directory.Delete(directory, true); }
    }

    private static async Task Capture(Window window, string name)
    {
        await Task.Delay(100); window.UpdateLayout();
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        var output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../TestResults/screenshots"));
        Directory.CreateDirectory(output); frame.Save(Path.Combine(output, name + ".png"));
    }
}
