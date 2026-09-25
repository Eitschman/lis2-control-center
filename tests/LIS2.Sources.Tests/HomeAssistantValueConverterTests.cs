using System.Text.Json;
using LIS2.Sources;

namespace LIS2.Sources.Tests;

public sealed class HomeAssistantValueConverterTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void Convert_BooleanPreservesType(string json, bool expected)
    {
        using var document = JsonDocument.Parse(json);
        Assert.Equal(expected, HomeAssistantValueConverter.Convert(document.RootElement));
    }

    [Fact]
    public void Convert_StringPreservesValue()
    {
        var json = JsonSerializer.Serialize("hello");
        using var document = JsonDocument.Parse(json);

        Assert.Equal("hello", HomeAssistantValueConverter.Convert(document.RootElement));
    }

    [Fact]
    public void Convert_IntegerRemainsInteger()
    {
        using var document = JsonDocument.Parse("42");
        Assert.Equal(42L, HomeAssistantValueConverter.Convert(document.RootElement));
    }

    [Fact]
    public void Convert_FloatingPointRemainsNumeric()
    {
        using var document = JsonDocument.Parse("42.5");
        Assert.Equal(42.5, HomeAssistantValueConverter.Convert(document.RootElement));
    }

    [Fact]
    public void Convert_ArrayUsesCompactJson()
    {
        var payload = JsonSerializer.Serialize(new[] { 1, 2, 3 });
        using var document = JsonDocument.Parse(payload);

        Assert.Equal("[1,2,3]", HomeAssistantValueConverter.Convert(document.RootElement));
    }

    [Fact]
    public void Convert_ObjectUsesCompactJson()
    {
        var payload = JsonSerializer.Serialize(new
        {
            temperature = 21,
            condition = "sunny"
        });
        using var document = JsonDocument.Parse(payload);

        Assert.Equal(
            payload,
            HomeAssistantValueConverter.Convert(document.RootElement));
    }

    [Fact]
    public void Convert_NullReturnsNull()
    {
        using var document = JsonDocument.Parse("null");
        Assert.Null(HomeAssistantValueConverter.Convert(document.RootElement));
    }

    [Fact]
    public void Convert_LargeStructuredAttributeRemainsValidJson()
    {
        var payload = JsonSerializer.Serialize(
            Enumerable.Range(0, 500)
                .Select(index => new { index, value = $"item-{index}" })
                .ToArray());

        using var document = JsonDocument.Parse(payload);
        var converted = Assert.IsType<string>(
            HomeAssistantValueConverter.Convert(document.RootElement));

        using var roundTrip = JsonDocument.Parse(converted);
        Assert.Equal(500, roundTrip.RootElement.GetArrayLength());
    }
}
