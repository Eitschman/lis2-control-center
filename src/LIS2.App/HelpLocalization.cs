namespace LIS2.App;

internal static partial class HelpLocalization
{
    internal static void ValidateCoverage()
    {
        var expected = German.Keys.ToHashSet(StringComparer.Ordinal);

        foreach (var (name, dictionary) in new[]
                 {
                     ("fr", French),
                     ("tr", Turkish),
                     ("ru", Russian)
                 })
        {
            var keys = dictionary.Keys.ToHashSet(StringComparer.Ordinal);
            if (!expected.SetEquals(keys))
            {
                var missing = string.Join(", ", expected.Except(keys).OrderBy(key => key));
                var extra = string.Join(", ", keys.Except(expected).OrderBy(key => key));
                throw new InvalidOperationException(
                    $"Help translation coverage mismatch for {name}. Missing: [{missing}] Extra: [{extra}]");
            }
        }
    }

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
