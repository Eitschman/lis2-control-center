using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using LIS2.App.Avalonia;
using LIS2.Core;

namespace LIS2.App.Avalonia.Tests;

public sealed class ConnectionTestUiTests
{
    [AvaloniaTheory]
    [InlineData("German", "de", 1305, 893)]
    [InlineData("English", "en", 1305, 893)]
    [InlineData("French", "fr", 1305, 893)]
    [InlineData("Turkish", "tr", 950, 600)]
    [InlineData("Russian", "ru", 950, 600)]
    public async Task TestFailureRetryCancellationAndHelp(string language, string code, int width, int height)
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-connection-ui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        await store.SaveAsync(new PortableWorkspace { LanguageMode = language });
        var serialCreations = 0;
        var serialTransport = new VirtualLis2Transport();
        var window = new MainWindow(store, new PortableHaTokenStore(directory, (_, _) => null), false,
            _ => { serialCreations++; return new SerialDisplaySession(serialTransport); }) { Width = width, Height = height };
        try
        {
            window.Show(); await Task.Delay(100);
            Click(window, "NavButton8");
            var picker = window.FindControl<ComboBox>("ConnectionTestSource")!;
            Assert.Equal(OperatingSystem.IsWindows() ? 7 : 5, picker.ItemCount);
            Assert.Equal(OperatingSystem.IsWindows(), picker.Items.Any(item => item?.ToString() == "Winamp"));
            picker.SelectedItem = picker.Items.Single(item => item?.ToString() == "Home Assistant");
            await using var server = new HomeAssistantServer();
            window.FindControl<TextBox>("HaUrl")!.Text = server.Url;
            window.FindControl<TextBox>("HaToken")!.Text = "fixture-token";
            var before = await File.ReadAllBytesAsync(store.FilePath);
            var failed = window.RunConnectionTestAsync();
            Assert.False(window.FindControl<Button>("ConnectionTestStart")!.IsEnabled);
            await server.AcceptAsync(false, "fixture-token secret://private");
            await failed;
            Assert.Contains(ConnectionTestText.Get("Home Assistant authentication was rejected", code), ResultText(window));
            Assert.DoesNotContain("fixture-token", ResultText(window));
            Assert.DoesNotContain("secret://private", ResultText(window));
            Assert.DoesNotContain("fixture-token", window.BuildDiagnosticsReport());
            Assert.Equal(before, await File.ReadAllBytesAsync(store.FilePath));
            Assert.Equal(0, serialCreations); Assert.Empty(serialTransport.Writes);
            await Capture(window, "connection-failure-" + code);
            if (code == "de")
            {
                window.FindControl<ComboBox>("ThemeModeComboBox")!.SelectedIndex = 2;
                await Capture(window, "connection-failure-dark-de");
            }
            var successful = window.RunConnectionTestAsync();
            await server.AcceptAsync(); await successful;
            Assert.Contains(ConnectionTestText.Get("Connection test passed", code), window.FindControl<TextBlock>("ConnectionTestStatus")!.Text);
            await Capture(window, "connection-success-" + code);
            var pending = window.RunConnectionTestAsync([new("WebSocket connection", "", async token =>
            { await Task.Delay(Timeout.InfiniteTimeSpan, token); return null; })]);
            Click(window, "ConnectionTestCancel"); await pending;
            Assert.Contains(ConnectionTestText.Get("Test cancelled", code), window.FindControl<TextBlock>("ConnectionTestStatus")!.Text);
            Assert.True(window.FindControl<Button>("ConnectionTestStart")!.IsEnabled);
            Assert.False(window.FindControl<Button>("ConnectionTestCancel")!.IsEnabled);
            Click(window, "OpenHelp");
            var help = window.OwnedWindows.Single(dialog => dialog.Name == "HelpDialog");
            help.Width = 840; help.Height = 600;
            var paragraph = help.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Text == ConnectionTestText.Get(ConnectionTestText.Help, code));
            paragraph.BringIntoView(); await Capture(help, "connection-help-" + code); help.Close();
            Click(window, "ConnectionTestOpen");
            Assert.True(window.FindControl<ScrollViewer>("HomeAssistantView")!.IsVisible);
            Click(window, "NavButton8");
            picker.SelectedIndex = 1;
            Assert.Empty(window.FindControl<StackPanel>("ConnectionTestResults")!.Children);
            await window.RunConnectionTestAsync();
            Assert.Equal(0, serialCreations); Assert.Empty(serialTransport.Writes);
        }
        finally { window.Close(); Directory.Delete(directory, true); }
    }

    [AvaloniaFact]
    public async Task LanguageChangesPreserveResultsAndShutdownCancelsInFlightTest()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-connection-shutdown-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var window = new MainWindow(new PortableWorkspaceStore(Path.Combine(directory, "workspace.json")),
            new PortableHaTokenStore(directory, (_, _) => null), false, _ => new SerialDisplaySession(new VirtualLis2Transport()));
        try
        {
            window.Show();
            await window.RunConnectionTestAsync([ConnectionTest.Check("Player session", false, "No connection to the selected player", "Open the source and check its settings")]);
            window.FindControl<ComboBox>("LanguageModeComboBox")!.SelectedIndex = 2;
            Assert.Contains("Playersitzung", ResultText(window));
            var task = window.RunConnectionTestAsync([new("WebSocket connection", "", async token => { await Task.Delay(Timeout.InfiniteTimeSpan, token); return null; })]);
            await window.BeginShutdownAsync().WaitAsync(TimeSpan.FromSeconds(3));
            Assert.True(task.IsCompleted);
        }
        finally { window.Close(); Directory.Delete(directory, true); }
    }

    private static string ResultText(MainWindow window) => string.Join("\n", window.FindControl<StackPanel>("ConnectionTestResults")!
        .GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text));
    private static void Click(MainWindow window, string name) => window.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static async Task Capture(Window window, string name)
    {
        await Task.Delay(100); window.UpdateLayout();
        using var frame = window.CaptureRenderedFrame(); Assert.NotNull(frame);
        var output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../TestResults/screenshots"));
        Directory.CreateDirectory(output); frame.Save(Path.Combine(output, name + ".png"));
    }
}
