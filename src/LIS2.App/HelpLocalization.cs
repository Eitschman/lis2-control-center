namespace LIS2.App;

internal static partial class HelpLocalization
{
    public static string Translate(string english)
    {
        if (string.IsNullOrEmpty(english))
            return english;

        var dictionary = LocalizationService.LanguageCode switch
        {
            "de" => German,
            "fr" => French,
            "tr" => Turkish,
            "ru" => Russian,
            _ => null
        };

        return dictionary is not null &&
               dictionary.TryGetValue(english, out var translated)
            ? translated
            : english;
    }
}
