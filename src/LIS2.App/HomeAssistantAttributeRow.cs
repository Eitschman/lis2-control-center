namespace LIS2.App;

public sealed record HomeAssistantAttributeRow(
    string Name,
    string Value,
    string TemplateKey,
    string? AliasTemplateKey)
{
    public string DisplayValue =>
        Value.Length <= 96
            ? Value
            : Value[..93] + "...";
}
