using System.Globalization;
using System.Windows;

namespace LIS2.App;

public static class LocalizationService
{
    public const string SystemLanguage = "System";
    public const string FallbackLanguage = "en";

    private static readonly HashSet<string> SupportedLanguages =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "en", "de", "fr", "tr", "ru"
        };

    public static string CurrentLanguage { get; private set; } = FallbackLanguage;

    public static string ResolveLanguage(string? configuredLanguage)
    {
        if (!string.IsNullOrWhiteSpace(configuredLanguage) &&
            !string.Equals(
                configuredLanguage,
                SystemLanguage,
                StringComparison.OrdinalIgnoreCase))
        {
            var explicitLanguage = Normalize(configuredLanguage);
            return SupportedLanguages.Contains(explicitLanguage)
                ? explicitLanguage
                : FallbackLanguage;
        }

        var systemLanguage = Normalize(CultureInfo.CurrentUICulture.Name);
        return SupportedLanguages.Contains(systemLanguage)
            ? systemLanguage
            : FallbackLanguage;
    }

    public static void Apply(string? configuredLanguage)
    {
        var language = ResolveLanguage(configuredLanguage);
        var resources = Application.Current?.Resources
            ?? throw new InvalidOperationException(
                "WPF application resources are not initialized.");

        var assemblyName =
            typeof(LocalizationService).Assembly.GetName().Name
            ?? "LIS2ControlCenter";

        var source = new Uri(
            $"/{assemblyName};component/Localization/Strings.{language}.xaml",
            UriKind.Relative);

        var dictionary = new ResourceDictionary { Source = source };

        var existingIndex = -1;
        for (var index = 0; index < resources.MergedDictionaries.Count; index++)
        {
            var existingSource =
                resources.MergedDictionaries[index].Source?.OriginalString;

            if (existingSource?.Contains(
                    "/Localization/Strings.",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                existingIndex = index;
                break;
            }
        }

        if (existingIndex >= 0)
            resources.MergedDictionaries[existingIndex] = dictionary;
        else
            resources.MergedDictionaries.Add(dictionary);

        CurrentLanguage = language;

        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
    }

    public static string Get(string key, string fallback)
    {
        var value = Application.Current?.TryFindResource(key);
        return value as string ?? fallback;
    }

    private static string Normalize(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return FallbackLanguage;

        var separator = cultureName.IndexOfAny(['-', '_']);
        return (separator >= 0
                ? cultureName[..separator]
                : cultureName)
            .ToLowerInvariant();
    }
}
