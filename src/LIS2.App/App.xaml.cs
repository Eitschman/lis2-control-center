using Microsoft.Win32;
using System.Windows;

namespace LIS2.App;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ApplySavedThemeBeforeWindowCreation();

        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        base.OnExit(e);
    }

    private static void ApplySavedThemeBeforeWindowCreation()
    {
        try
        {
            var settings = new SettingsStore()
                .LoadAsync()
                .GetAwaiter()
                .GetResult();

            var mode = Enum.TryParse<AppThemeMode>(
                settings.ThemeMode,
                ignoreCase: true,
                out var parsed)
                ? parsed
                : AppThemeMode.System;

            ThemeService.Apply(mode);
        }
        catch
        {
            ThemeService.Apply(AppThemeMode.System);
        }
    }

    private void SystemEvents_UserPreferenceChanged(
        object sender,
        UserPreferenceChangedEventArgs e)
    {
        if (ThemeService.Mode != AppThemeMode.System)
            return;

        Dispatcher.Invoke(ThemeService.RefreshSystemTheme);
    }
}
