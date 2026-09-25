using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LIS2.App;

public static class ThemeService
{
    private const string PersonalizeKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private const string AppsUseLightThemeValue = "AppsUseLightTheme";
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmUseImmersiveDarkModeBefore20H1 = 19;

    private static AppThemeMode _mode = AppThemeMode.System;

    public static AppThemeMode Mode => _mode;

    public static bool IsDarkEffective =>
        _mode == AppThemeMode.Dark ||
        (_mode == AppThemeMode.System && !WindowsUsesLightTheme());

    public static void Apply(AppThemeMode mode)
    {
        _mode = mode;
        ApplyEffectiveTheme();
    }

    public static void RefreshSystemTheme()
    {
        if (_mode == AppThemeMode.System)
            ApplyEffectiveTheme();
    }

    public static void ApplyWindowChrome(Window? window)
    {
        if (window is null)
            return;

        try
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
                return;

            var enabled = IsDarkEffective ? 1 : 0;

            if (DwmSetWindowAttribute(
                    handle,
                    DwmUseImmersiveDarkMode,
                    ref enabled,
                    sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(
                    handle,
                    DwmUseImmersiveDarkModeBefore20H1,
                    ref enabled,
                    sizeof(int));
            }
        }
        catch
        {
            // Title-bar theming is cosmetic and must never break the app.
        }
    }

    private static void ApplyEffectiveTheme()
    {
        var useLight =
            _mode == AppThemeMode.Light ||
            (_mode == AppThemeMode.System && WindowsUsesLightTheme());

        var themeName = useLight ? "Light.xaml" : "Dark.xaml";
        var assemblyName =
            typeof(ThemeService).Assembly.GetName().Name
            ?? "LIS2ControlCenter";

        var source = new Uri(
            $"/{assemblyName};component/Themes/{themeName}",
            UriKind.Relative);

        var resources = System.Windows.Application.Current?.Resources
            ?? throw new InvalidOperationException(
                "WPF application resources are not initialized.");

        var dictionaries = resources.MergedDictionaries;
        var palette = new ResourceDictionary { Source = source };

        if (dictionaries.Count == 0)
            dictionaries.Add(palette);
        else
            dictionaries[0] = palette;

        foreach (Window window in System.Windows.Application.Current.Windows)
            ApplyWindowChrome(window);
    }

    private static bool WindowsUsesLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);

            return key?.GetValue(AppsUseLightThemeValue) switch
            {
                int value => value != 0,
                _ => true
            };
        }
        catch
        {
            return true;
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int value,
        int valueSize);
}
