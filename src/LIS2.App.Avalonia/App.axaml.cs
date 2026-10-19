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
            var window = new MainWindow();
            desktop.MainWindow = window;
            if (OperatingSystem.IsWindows())
                ConfigureWindowsTray(desktop, window);
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureWindowsTray(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var iconBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAoUlEQVR4nGOUUDH8zzCAgGkgLR91wKgDBoUDWLAJBrQfoYllGyptMMQwQoBWluMymxG5IIIpwOZSajoA2fzBmQZg4Jd4JFUsYXu5HKfcgIfAqAPwpgFk8PZIKVZxYZtuouRxgaETAoR8QkgeFxjwEBh1AN40gK8EoxYY8BBgRG+W07I6ZmDArGkxQoBWVTEuszFCgN5gwNPAqANGHTDgDgAAoqskQcLla/AAAAAASUVORK5CYII=");
        using var stream = new MemoryStream(iconBytes);
        var icon = new WindowIcon(stream);
        window.Icon = icon;
        var menu = new NativeMenu();
        var show = new NativeMenuItem("Open LIS2 Control Center");
        show.Click += (_, _) => RestoreWindow(window);
        menu.Add(show);
        var exit = new NativeMenuItem("Exit");
        exit.Click += (_, _) =>
        {
            _exitRequested = true;
            desktop.Shutdown();
        };
        menu.Add(exit);
        _trayIcon = new TrayIcon { Icon = icon, ToolTipText = "LIS2 Control Center", Menu = menu, IsVisible = true };
        _trayIcon.Clicked += (_, _) => RestoreWindow(window);
        TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });

        window.PropertyChanged += (_, args) =>
        {
            if (args.Property == Window.WindowStateProperty && window.WindowState == WindowState.Minimized)
                window.Hide();
        };
        window.Closing += (_, args) =>
        {
            if (_exitRequested) return;
            args.Cancel = true;
            window.Hide();
        };
    }

    private static void RestoreWindow(MainWindow window)
    {
        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
    }
}
