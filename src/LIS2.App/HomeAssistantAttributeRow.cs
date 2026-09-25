namespace LIS2.App;

public sealed record HomeAssistantAttributeRow(
    string Name,
    string Value,
    string TemplateKey,
    string? AliasTemplateKey);
