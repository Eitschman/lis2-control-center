using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;

namespace LIS2.App.Avalonia;

public partial class App : Application
{
    internal static DesktopInstance? Instance { get; set; }
    private TrayIcon? _trayIcon;
    private bool _exitRequested;
    private bool _smokeStarted;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args ?? Array.Empty<string>();
            var smokeIndex = Array.IndexOf(args, "--desktop-smoke");
            var smokeDirectory = smokeIndex >= 0 && smokeIndex + 1 < args.Length
                ? Path.GetFullPath(args[smokeIndex + 1]) : null;
            var splash = new StartupSplash();
            desktop.MainWindow = splash;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            splash.Opened += (_, _) => global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var main = smokeDirectory is null ? new MainWindow() : DesktopSmoke.CreateWindow(smokeDirectory);
                    desktop.MainWindow = main;
                    ConfigureDesktop(desktop, main);
                    Instance?.Listen(() => global::Avalonia.Threading.Dispatcher.UIThread.Post(() => RestoreWindow(main)));
                    if (smokeDirectory is not null)
                        main.Opened += async (_, _) => await RunSmokeAsync(desktop, main, smokeDirectory);
                    main.Show();
                    splash.Close();
                }
                catch (Exception exception)
                {
                    FailureLog.Write("Startup", exception);
                    System.Diagnostics.Trace.TraceError("Startup failed: {0}", exception);
                    if (smokeDirectory is not null)
                    {
                        Directory.CreateDirectory(smokeDirectory);
                        File.WriteAllText(Path.Combine(smokeDirectory, "failure.txt"), exception.ToString());
                        desktop.Shutdown(1);
                    }
                    else
                    {
                        var error = new Window
                        {
                            Title = "LIS2 Control Center",
                            Width = 620,
                            Height = 240,
                            Content = new TextBox { Text = exception.Message, IsReadOnly = true, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap }
                        };
                        desktop.MainWindow = error;
                        error.Closed += (_, _) => desktop.Shutdown(1);
                        error.Show();
                        splash.Close();
                    }
                }
            }, global::Avalonia.Threading.DispatcherPriority.Background);
        }
        base.OnFrameworkInitializationCompleted();
    }

    private async Task RunSmokeAsync(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window, string smokeDirectory)
    {
        if (_smokeStarted) return;
        _smokeStarted = true;
        var exitCode = 0;
        try
        {
            if (_trayIcon?.Menu?.Items.Count != 2) throw new InvalidOperationException("Tray menu missing.");
            var languages = window.FindControl<ComboBox>("LanguageModeComboBox")!;
            for (var language = 1; language <= 5; language++)
            {
                languages.SelectedIndex = language;
                var labels = _trayIcon.Menu.Items.OfType<NativeMenuItem>().Select(item => item.Header).ToArray();
                if (!labels.SequenceEqual(new[] { window.UiText("Open LIS2 Control Center"), window.UiText("Exit") }))
                    throw new InvalidOperationException("Tray language mismatch: " + language);
            }
            await DesktopSmoke.RunAsync(window, smokeDirectory);
        }
        catch (Exception exception)
        {
            exitCode = 1;
            Directory.CreateDirectory(smokeDirectory);
            await File.WriteAllTextAsync(Path.Combine(smokeDirectory, "failure.txt"), exception.ToString());
        }
        await ExitAsync(desktop, window, exitCode);
    }

    private void ConfigureDesktop(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        window.CloseToTray = OperatingSystem.IsWindows();
        window.Closed += async (_, _) => { if (!_exitRequested) await ExitAsync(desktop, window); };
        desktop.ShutdownRequested += async (_, args) =>
        {
            if (_exitRequested) return;
            args.Cancel = true;
            await ExitAsync(desktop, window);
        };
        if (Environment.GetCommandLineArgs().Contains("--minimized", StringComparer.OrdinalIgnoreCase))
        {
            window.Opened += MinimizeOnStartup;
            void MinimizeOnStartup(object? sender, EventArgs args)
            {
                window.Opened -= MinimizeOnStartup;
                if (OperatingSystem.IsWindows()) window.Hide();
                else window.WindowState = WindowState.Minimized;
            }
        }
        var iconBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAoUlEQVR4nGOUUDH8zzCAgGkgLR91wKgDBoUDWLAJBrQfoYllGyptMMQwQoBWluMymxG5IIIpwOZSajoA2fzBmQZg4Jd4JFUsYXu5HKfcgIfAqAPwpgFk8PZIKVZxYZtuouRxgaETAoR8QkgeFxjwEBh1AN40gK8EoxYY8BBgRG+W07I6ZmDArGkxQoBWVTEuszFCgN5gwNPAqANGHTDgDgAAoqskQcLla/AAAAAASUVORK5CYII=");
        using var stream = new MemoryStream(iconBytes);
        var icon = new WindowIcon(stream);
        window.Icon = icon;
        var menu = new NativeMenu();
        var show = new NativeMenuItem("Open LIS2 Control Center");
        show.Click += (_, _) => RestoreWindow(window);
        menu.Add(show);
        var exit = new NativeMenuItem("Exit");
        exit.Click += async (_, _) => await ExitAsync(desktop, window);
        menu.Add(exit);
        void LocalizeTray()
        {
            show.Header = window.UiText("Open LIS2 Control Center");
            exit.Header = window.UiText("Exit");
        }
        window.UiLanguageChanged += LocalizeTray;
        LocalizeTray();
        _trayIcon = new TrayIcon { Icon = icon, ToolTipText = "LIS2 Control Center", Menu = menu, IsVisible = true };
        _trayIcon.Clicked += (_, _) => RestoreWindow(window);
        TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });

        window.PropertyChanged += (_, args) =>
        {
            // Unter Linux bleibt das Fenster über die Taskleiste erreichbar, auch ohne Tray-Host.
            if (OperatingSystem.IsWindows() && args.Property == Window.WindowStateProperty && window.WindowState == WindowState.Minimized)
                window.Hide();
        };
    }

    private bool _exiting;

    private async Task ExitAsync(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window, int exitCode = 0)
    {
        if (_exiting) return;
        _exiting = true;
        try { await window.BeginShutdownAsync(); }
        catch (Exception exception)
        {
            exitCode = 1;
            System.Diagnostics.Trace.TraceError("Shutdown failed: {0}", exception);
        }
        finally
        {
            _trayIcon?.Dispose();
            _exitRequested = true;
            desktop.Shutdown(exitCode);
        }
    }

    private static void RestoreWindow(MainWindow window)
    {
        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
    }
}
