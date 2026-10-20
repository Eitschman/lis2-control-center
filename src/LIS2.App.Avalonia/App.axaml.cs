using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;

namespace LIS2.App.Avalonia;

public partial class App : Application
{
    private TrayIcon? _trayIcon;
    private bool _exitRequested;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args ?? Array.Empty<string>();
            var smokeIndex = Array.IndexOf(args, "--desktop-smoke");
            var smokeDirectory = smokeIndex >= 0 && smokeIndex + 1 < args.Length
                ? Path.GetFullPath(args[smokeIndex + 1]) : null;
            var window = smokeDirectory is null ? new MainWindow() : DesktopSmoke.CreateWindow(smokeDirectory);
            desktop.MainWindow = window;
            desktop.ShutdownRequested += async (_, args) =>
            {
                if (_exitRequested) return;
                args.Cancel = true;
                await ExitAsync(desktop, window);
            };
            if (smokeDirectory is not null)
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                window.Opened += async (_, _) =>
                {
                    var exitCode = 0;
                    try { await DesktopSmoke.RunAsync(window, smokeDirectory); }
                    catch (Exception exception)
                    {
                        exitCode = 1;
                        Directory.CreateDirectory(smokeDirectory);
                        await File.WriteAllTextAsync(Path.Combine(smokeDirectory, "failure.txt"), exception.ToString());
                    }
                    await ExitAsync(desktop, window, exitCode);
                };
            }
            else if (OperatingSystem.IsWindows())
            {
                ConfigureWindowsTray(desktop, window);
                if (Environment.GetCommandLineArgs().Contains("--minimized", StringComparer.OrdinalIgnoreCase))
                    window.Opened += (_, _) => window.Hide();
            }
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureWindowsTray(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        window.CloseToTray = true;
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
        _trayIcon = new TrayIcon { Icon = icon, ToolTipText = "LIS2 Control Center", Menu = menu, IsVisible = true };
        _trayIcon.Clicked += (_, _) => RestoreWindow(window);
        TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });

        window.PropertyChanged += (_, args) =>
        {
            if (args.Property == Window.WindowStateProperty && window.WindowState == WindowState.Minimized)
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
