using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using LIS2.Core;

namespace LIS2.App.Avalonia.Tests;

public sealed class ProtocolTesterUiTests
{
    [AvaloniaFact]
    public async Task VirtualCommandsProduceProtocolBytesAndDisconnectRejectsWrites()
    {
        var window = new ProtocolTester.MainWindow();
        window.Show();
        try
        {
            T Control<T>(string name) where T : Control => window.FindControl<T>(name)!;
            async Task Click(string name)
            {
                Control<Button>(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(40);
            }
            await Click("ConnectButton");
            Assert.Contains("Virtual LIS2", Control<TextBlock>("ConnectionStatus").Text);
            Control<TextBox>("Line1TextBox").Text = "ÄÖÜ LIS2";
            Control<TextBox>("Line2TextBox").Text = "Protocol test";
            await Click("SendLine1Button");
            await Click("SendLine2Button");
            await Click("SendFansButton");
            await Click("ProgramCharacterButton");
            await Click("ClearDisplayButton");
            foreach (var button in window.GetVisualDescendants().OfType<Button>().Where(button => button.Tag is string))
            {
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(40);
                Assert.Contains("Brightness " + button.Tag + "%", Control<TextBox>("LogTextBox").Text);
            }
            var log = Control<TextBox>("LogTextBox").Text!;
            Assert.Contains("Line1 \"ÄÖÜ LIS2\"", log);
            Assert.Contains("Line2 \"Protocol test\"", log);
            Assert.Contains("Fans 100/100/100/100%", log);
            Assert.Contains("Program character slot 1: 00 04 0E 1F 0E 04 00 00", log);
            Assert.Contains("VIRT " + string.Join(" ", Lis2Protocol.SetFans(100, 100, 100, 100).Select(value => value.ToString("X2"))), log);
            Assert.Contains("VIRT A0", log);
            window.Width = 920;
            window.Height = 720;
            await Task.Delay(80);
            using (var frame = window.CaptureRenderedFrame())
            {
                Assert.NotNull(frame);
                frame.Save(ScreenshotPath("protocol-tester.png"));
            }
            window.Width = 820;
            window.Height = 640;
            await Task.Delay(80);
            Assert.True(Control<TextBox>("LogTextBox").Bounds.Height >= 40);
            using (var frame = window.CaptureRenderedFrame())
            {
                Assert.NotNull(frame);
                frame.Save(ScreenshotPath("protocol-tester-minimum.png"));
            }
            await Click("ConnectButton");
            await Click("ClearDisplayButton");
            Assert.Contains("Connect to the LIS2 transport first", Control<TextBlock>("ErrorText").Text);
            await Click("ClearLogButton");
            Assert.Equal(string.Empty, Control<TextBox>("LogTextBox").Text);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("Fan1TextBox", "101", "SendFansButton", "between 0 and 100")]
    [InlineData("Fan2TextBox", "invalid", "SendFansButton", "between 0 and 100")]
    [InlineData("CharacterSlotTextBox", "0", "ProgramCharacterButton", "between 1 and 8")]
    [InlineData("CharacterRowsTextBox", "00", "ProgramCharacterButton", "exactly 8")]
    [InlineData("CharacterRowsTextBox", "20 00 00 00 00 00 00 00", "ProgramCharacterButton", "between 00 and 1F")]
    [InlineData("CharacterRowsTextBox", "ZZ 00 00 00 00 00 00 00", "ProgramCharacterButton", "format")]
    public async Task InvalidInputReportsErrorWithoutWriting(string field, string value, string action, string error)
    {
        var window = new ProtocolTester.MainWindow();
        window.Show();
        try
        {
            window.FindControl<Button>("ConnectButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(40);
            window.FindControl<TextBox>("LogTextBox")!.Text = string.Empty;
            window.FindControl<TextBox>(field)!.Text = value;
            window.FindControl<Button>(action)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(40);
            Assert.NotEmpty(window.FindControl<TextBlock>("ErrorText")!.Text!);
            if (value != "ZZ 00 00 00 00 00 00 00")
                Assert.Contains(error, window.FindControl<TextBlock>("ErrorText")!.Text);
            Assert.DoesNotContain("VIRT", window.FindControl<TextBox>("LogTextBox")!.Text!);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task SerialSelectionRequiresPortWithoutSwitchingToVirtual()
    {
        var window = new ProtocolTester.MainWindow();
        window.Show();
        try
        {
            window.FindControl<CheckBox>("VirtualModeCheckBox")!.IsChecked = false;
            window.FindControl<ComboBox>("PortComboBox")!.ItemsSource = Array.Empty<string>();
            window.FindControl<Button>("ConnectButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(40);
            Assert.Contains("Select a serial port", window.FindControl<TextBlock>("ErrorText")!.Text);
            Assert.False(window.FindControl<CheckBox>("VirtualModeCheckBox")!.IsChecked);
            Assert.DoesNotContain("VIRT", window.FindControl<TextBox>("LogTextBox")!.Text!);
        }
        finally { window.Close(); }
    }

    private static string ScreenshotPath(string name)
    {
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../TestResults/screenshots"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, name);
    }
}
