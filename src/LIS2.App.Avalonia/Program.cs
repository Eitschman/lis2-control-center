using Avalonia;

namespace LIS2.App.Avalonia;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args is ["--media-provider-smoke", var directory])
            return WindowsMediaSmoke.RunAsync(Path.GetFullPath(directory)).GetAwaiter().GetResult();
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
