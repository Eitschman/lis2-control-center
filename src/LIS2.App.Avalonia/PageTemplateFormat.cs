namespace LIS2.App.Avalonia;

/// <summary>Erzeugt die vorhandene Vorlagensyntax, ohne Eingaben als zusätzliche Operationen auszuwerten.</summary>
internal sealed record PageTemplateFormat(string Key, int Kind, int Decimals, int LetterCase,
    string Prefix, string Suffix, string Fallback, string FallbackVariable = "")
{
    public bool TryBuild(out string template)
    {
        template = string.Empty;
        if (string.IsNullOrWhiteSpace(Key) || Key.Any(character => char.IsWhiteSpace(character) || "{}|".Contains(character)) ||
            new[] { Prefix, Suffix, Fallback }.Any(value => value.IndexOfAny(['{', '}', '|']) >= 0) ||
            FallbackVariable.Any(character => char.IsWhiteSpace(character) || "{}|".Contains(character)) ||
            Kind is < 0 or > 3 || LetterCase is < 0 or > 2 || Decimals is < 0 or > 6)
            return false;
        var parts = new List<string> { Key };
        if (FallbackVariable.Length > 0) parts.Add("fallbackvar:" + FallbackVariable);
        if (Fallback.Length > 0) parts.Add("fallback:" + Fallback);
        if (Kind > 0) parts.Add(new[] { "", "number", "percent", "bytes" }[Kind] + ":" + Decimals);
        if (LetterCase > 0) parts.Add(LetterCase == 1 ? "upper" : "lower");
        if (Prefix.Length > 0) parts.Add("prefix:" + Prefix);
        if (Suffix.Length > 0) parts.Add("suffix:" + Suffix);
        template = "{" + string.Join('|', parts) + "}";
        return true;
    }
}
