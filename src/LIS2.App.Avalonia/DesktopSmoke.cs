using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace LIS2.App.Avalonia;

/// <summary>Isolierter Desktop-Smoke mit echten Fenstern und virtuellem Gerät, ohne Benutzerkonfiguration.</summary>
internal static class DesktopSmoke
{
    internal static readonly string[] Views =
        ["Dashboard", "Display", "Pages", "Media", "Events", "Hardware", "FanControl", "Settings", "Diagnostics", "HomeAssistant"];

    public static MainWindow CreateWindow(string directory) => new(
        new PortableWorkspaceStore(Path.Combine(directory, "workspace.json")),
        new PortableHaTokenStore(directory, "lis2-smoke-no-secret-service"), false,
        _ => throw new InvalidOperationException("Serial hardware is disabled during desktop smoke."));

    public static async Task RunAsync(MainWindow window, string directory, bool crossProcessActivation = true)
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "scaling.txt"),
            window.RenderScaling.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (double.TryParse(Environment.GetEnvironmentVariable("LIS2_SMOKE_EXPECT_SCALE"),
            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var expectedScale) &&
            Math.Abs(window.RenderScaling - expectedScale) > 0.01)
            throw new InvalidOperationException("Unexpected desktop rendering scale: " + window.RenderScaling);
        var splash = new StartupSplash();
        splash.Show();
        await Task.Delay(80);
        Capture(splash, Path.Combine(directory, "Splash.png"));
        splash.Close();
        using var instance = DesktopInstance.TryAcquire(Path.Combine(directory, "instance"))
            ?? throw new InvalidOperationException("Smoke instance lease unavailable.");
        var activated = false;
        instance.Listen(() => global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            window.Show(); window.WindowState = WindowState.Normal; window.Activate(); activated = true;
        }));
        window.Hide();
        if (crossProcessActivation)
        {
            var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
            if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase))
                start.ArgumentList.Add(typeof(DesktopSmoke).Assembly.Location);
            start.ArgumentList.Add("--instance-activation-smoke");
            start.ArgumentList.Add(Path.GetFullPath(Path.Combine(directory, "instance")));
            using (var child = System.Diagnostics.Process.Start(start)!)
            {
                try { await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); }
                catch { if (!child.HasExited) child.Kill(true); throw; }
                if (child.ExitCode != 0) throw new InvalidOperationException("Cross-process desktop activation failed: " + child.ExitCode);
            }
        }
        else if (!await DesktopInstance.ActivateAsync(Path.Combine(directory, "instance")))
            throw new InvalidOperationException("Desktop activation failed.");
        await WaitAsync(() => activated && window.IsVisible);
        // Der Smoke prüft englische Meldungen; Sprachvarianten werden getrennt geprüft.
        window.FindControl<ComboBox>("LanguageModeComboBox")!.SelectedIndex = 1;
        await WaitAsync(() => window.FindControl<TextBlock>("ConnectionText")!.Text?.StartsWith("Connected") == true &&
            window.FindControl<Button>("SettingsApplyTransport")!.IsEnabled);
        foreach (var (view, index) in Views.Select((view, index) => (view, index)))
        {
            Click(window, "NavButton" + index);
            await Task.Delay(40);
            if (!window.FindControl<ScrollViewer>(view + "View")!.IsEffectivelyVisible)
                throw new InvalidOperationException("View is not visible: " + view);
            Capture(window, Path.Combine(directory, view + ".png"));
        }
        foreach (var dialogName in new[] { "Help", "About" })
        {
            Click(window, "Open" + dialogName);
            await WaitAsync(() => window.OwnedWindows.Any(dialog => dialog.Name == dialogName + "Dialog"));
            var dialog = window.OwnedWindows.Single(dialog => dialog.Name == dialogName + "Dialog");
            await Task.Delay(40);
            Capture(dialog, Path.Combine(directory, dialogName + ".png"));
            dialog.Close();
            await WaitAsync(() => !window.OwnedWindows.Any());
        }
        Click(window, "NavButton3");
        var provider = OperatingSystem.IsWindows() ? "WindowsMedia" : "Media";
        await window.StartMediaProviderSafeAsync(provider, async () =>
        {
            if (OperatingSystem.IsWindows())
            {
                await using var bridge = new WindowsMediaBridge();
                await bridge.StartAsync(Path.Combine(directory, "missing-provider.dll"));
            }
            else await new LinuxMprisSource("lis2-smoke-missing-playerctl").ReadAsync();
        });
        var statusName = OperatingSystem.IsWindows() ? "WindowsMediaStatus" : "LinuxMediaStatus";
        if (window.FindControl<TextBlock>(statusName)!.Text?.StartsWith("Provider startup error:") != true)
            throw new InvalidOperationException("Missing media provider was not reported.");
        Capture(window, Path.Combine(directory, "MissingMediaProvider.png"));
        var report = window.BuildDiagnosticsReport();
        if (!report.Contains("OK Write", StringComparison.Ordinal))
            throw new InvalidOperationException("Protocol journal is empty.");
        await File.WriteAllTextAsync(Path.Combine(directory, "diagnostics.txt"), report);
        await window.BeginShutdownAsync().WaitAsync(TimeSpan.FromSeconds(15));
        var shutdownReport = window.BuildDiagnosticsReport();
        if (!shutdownReport.Contains("Virtual transport: Disconnected", StringComparison.Ordinal) ||
            !shutdownReport.Contains("Virtual page playback: Stopped", StringComparison.Ordinal))
            throw new InvalidOperationException("Runtime is still active after shutdown.");
        await File.WriteAllTextAsync(Path.Combine(directory, "shutdown.txt"), shutdownReport);
        await File.WriteAllTextAsync(Path.Combine(directory, "result.txt"), "PASS: splash, tray menu, instance activation, 10 views, Help, About, missing media provider, diagnostics, shutdown drained.");
    }

    private static void Click(Window window, string name) =>
        window.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task WaitAsync(Func<bool> predicate)
    {
        var timeout = DateTime.UtcNow.AddSeconds(10);
        while (!predicate())
        {
            if (DateTime.UtcNow >= timeout) throw new TimeoutException("Desktop smoke timed out.");
            await Task.Delay(20);
        }
    }

    private static void Capture(Window window, string path)
    {
        window.UpdateLayout();
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height));
        bitmap.Render(window);
        bitmap.Save(path);
    }
}
