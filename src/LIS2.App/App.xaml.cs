using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace LIS2.App;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        try
        {
            ApplySavedThemeBeforeWindowCreation();

            try
            {
                SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            }
            catch (Exception ex)
            {
                WriteStartupError("Unable to subscribe to Windows theme changes.", ex);
            }

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            WriteStartupError("Application startup failed.", ex);
            ShowStartupError(ex);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        }
        catch
        {
        }

        AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        DispatcherUnhandledException -= App_DispatcherUnhandledException;

        base.OnExit(e);
    }

    private static void ApplySavedThemeBeforeWindowCreation()
    {
        AppThemeMode mode;

        try
        {
            var settings = new SettingsStore().Load();

            mode = Enum.TryParse<AppThemeMode>(
                settings.ThemeMode,
                ignoreCase: true,
                out var parsed)
                ? parsed
                : AppThemeMode.System;
        }
        catch (Exception ex)
        {
            WriteStartupError(
                "Unable to load settings. Falling back to the built-in dark theme.",
                ex);
            return;
        }

        try
        {
            ThemeService.Apply(mode);
        }
        catch (Exception ex)
        {
            // App.xaml already contains the built-in Dark palette.
            // Do not retry theme loading here: a broken resource URI must
            // never prevent the application from opening.
            WriteStartupError(
                $"Unable to apply theme '{mode}'. Using the built-in dark theme.",
                ex);
        }
    }

    private void SystemEvents_UserPreferenceChanged(
        object sender,
        UserPreferenceChangedEventArgs e)
    {
        if (ThemeService.Mode != AppThemeMode.System)
            return;

        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                ThemeService.RefreshSystemTheme();
            }
            catch (Exception ex)
            {
                WriteStartupError(
                    "Unable to refresh the Windows system theme.",
                    ex);
            }
        });
    }

    private void App_DispatcherUnhandledException(
        object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        WriteStartupError("Unhandled UI exception.", e.Exception);
    }

    private static void CurrentDomain_UnhandledException(
        object sender,
        UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            WriteStartupError("Unhandled application exception.", ex);
    }

    private static void ShowStartupError(Exception ex)
    {
        try
        {
            System.Windows.MessageBox.Show(
                "LIS2 Control Center could not start.\n\n" +
                "A diagnostic log was written to:\n" +
                GetStartupErrorPath() +
                "\n\n" +
                ex.Message,
                "LIS2 Control Center",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
        }
    }

    private static void WriteStartupError(string context, Exception ex)
    {
        try
        {
            var path = GetStartupErrorPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            File.AppendAllText(
                path,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {context}{Environment.NewLine}" +
                $"{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    private static string GetStartupErrorPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LIS2ControlCenter",
            "startup-error.log");
}
