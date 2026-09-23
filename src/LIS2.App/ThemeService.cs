using Microsoft.Win32;
using System.Windows;

namespace LIS2.App;

public static class ThemeService
{
    private const string PersonalizeKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private const string AppsUseLightThemeValue = "AppsUseLightTheme";

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

    private static void ApplyEffectiveTheme()
    {
        var useLight =
            _mode == AppThemeMode.Light ||
            (_mode == AppThemeMode.System && WindowsUsesLightTheme());

        var source = new Uri(
            useLight ? "Themes/Light.xaml" : "Themes/Dark.xaml",
            UriKind.Relative);

        var dictionaries = Application.Current.Resources.MergedDictionaries;

        if (dictionaries.Count == 0)
        {
            dictionaries.Add(new ResourceDictionary { Source = source });
            return;
        }

        dictionaries[0] = new ResourceDictionary { Source = source };
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
}
