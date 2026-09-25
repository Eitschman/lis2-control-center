using System.Text.Json;

namespace LIS2.Sources;

public static class HomeAssistantValueConverter
{
    public static object? Convert(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number when value.TryGetDouble(out var number) => number,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Array or JsonValueKind.Object => value.GetRawText(),
            _ => value.GetRawText()
        };
}