using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace LIS2.ProtocolTester;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect();
}

public sealed class App : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            desktop.MainWindow = window;
            if (desktop.Args is ["--smoke-test", var directory])
                window.Opened += (_, _) => Dispatcher.UIThread.Post(async () =>
                {
                    var exitCode = 0;
                    Directory.CreateDirectory(directory);
                    try { await ProtocolSmoke.RunAsync(window, directory); }
                    catch (Exception exception)
                    {
                        await File.WriteAllTextAsync(Path.Combine(directory, "result.txt"), exception.ToString());
                        exitCode = 1;
                    }
                    desktop.Shutdown(exitCode);
                });
        }
        base.OnFrameworkInitializationCompleted();
    }
}

/// <summary>Prüft die native Oberfläche ausschließlich mit virtuellem LIS2 und sichert den sichtbaren Zustand.</summary>
internal static class ProtocolSmoke
{
    public static async Task RunAsync(MainWindow window, string directory)
    {
        async Task Click(string name)
        {
            window.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(80);
        }
        await Click("ConnectButton");
        window.FindControl<TextBox>("Line1TextBox")!.Text = "ÄÖÜ LIS2";
        window.FindControl<TextBox>("Line2TextBox")!.Text = "Avalonia tester";
        foreach (var name in new[] { "SendLine1Button", "SendLine2Button", "SendFansButton", "ProgramCharacterButton", "ClearDisplayButton" })
            await Click(name);
        var log = window.FindControl<TextBox>("LogTextBox")!.Text!;
        if (!log.Contains("VIRT A0") || !log.Contains("Fans 100/100/100/100%") || log.Contains("ERR"))
            throw new InvalidOperationException("Virtual protocol command smoke failed: " + log);
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width, (int)window.ClientSize.Height));
        bitmap.Render(window);
        bitmap.Save(Path.Combine(directory, "protocol-tester.png"));
        window.FindControl<TextBox>("Fan1TextBox")!.Text = "101";
        await Click("SendFansButton");
        if (string.IsNullOrEmpty(window.FindControl<TextBlock>("ErrorText")!.Text))
            throw new InvalidOperationException("Invalid fan value was accepted.");
        await Click("ConnectButton");
        await File.WriteAllTextAsync(Path.Combine(directory, "protocol.txt"), log);
        await File.WriteAllTextAsync(Path.Combine(directory, "result.txt"), "PASS: virtual commands, validation, disconnect");
    }
}
