using Avalonia;

namespace LIS2.App.Avalonia;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args is ["--apply-update", var planPath])
            return UpdateInstallation.RunWindowsHelperAsync(Path.GetFullPath(planPath)).GetAwaiter().GetResult();
        AppDomain.CurrentDomain.UnhandledException += (_, failure) =>
        {
            if (failure.ExceptionObject is Exception exception) FailureLog.Write("Unhandled", exception);
        };
        TaskScheduler.UnobservedTaskException += (_, failure) => FailureLog.Write("Unobserved task", failure.Exception);
        UpdateStartup.Configure(args);
        if (args is ["--media-provider-smoke", var directory])
            return WindowsMediaSmoke.RunAsync(Path.GetFullPath(directory)).GetAwaiter().GetResult();
        if (args is ["--instance-activation-smoke", var profileDirectory])
        {
            using var unexpectedLease = DesktopInstance.TryAcquire(profileDirectory);
            if (unexpectedLease is not null) return 2;
            return DesktopInstance.ActivateAsync(profileDirectory).GetAwaiter().GetResult() ? 0 : 1;
        }
        if (!args.Contains("--desktop-smoke", StringComparer.Ordinal))
        {
            var profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LIS2ControlCenter", "Avalonia");
            using var instance = DesktopInstance.TryAcquire(profile);
            if (instance is null) return DesktopInstance.ActivateAsync(profile).GetAwaiter().GetResult() ? 0 : 1;
            App.Instance = instance;
            try { return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args); }
            finally { App.Instance = null; }
        }
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
