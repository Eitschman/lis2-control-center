using System.Text;
using LIS2.Core;

namespace LIS2.Core.Tests;

public sealed class Lis2ProtocolTests
{
    [Fact]
    public void Clear_Is_A0() =>
        Assert.Equal(new byte[] { 0xA0 }, Lis2Protocol.Clear.ToArray());

    [Fact]
    public void Line1_Hello_EncodesExpectedBytes()
    {
        var actual = Lis2Protocol.WriteLine(1, 0, "HELLO");
        Assert.Equal(new byte[] { 0xA1, 0x00, 0xA7, 0x48, 0x45, 0x4C, 0x4C, 0x4F }, actual);
    }

    [Fact]
    public void Line2_AtColumn5_UsesA2()
    {
        var actual = Lis2Protocol.WriteLine(2, 5, "ABC");
        Assert.Equal(new byte[] { 0xA2, 0x05, 0xA7, 0x41, 0x42, 0x43 }, actual);
    }

    [Theory]
    [InlineData(Lis2Brightness.Percent100, 0x38)]
    [InlineData(Lis2Brightness.Percent75, 0x39)]
    [InlineData(Lis2Brightness.Percent50, 0x3A)]
    [InlineData(Lis2Brightness.Percent25, 0x3B)]
    public void Brightness_EncodesExpectedValue(Lis2Brightness brightness, byte value) =>
        Assert.Equal(new byte[] { 0xA5, value }, Lis2Protocol.SetBrightness(brightness));

    [Fact]
    public void Fans_EncodeDirectPercentages() =>
        Assert.Equal(
            new byte[] { 0xAE, 0xF0, 0x32, 0x32, 0x4B, 0x64 },
            Lis2Protocol.SetFans(50, 50, 75, 100));

    [Fact]
    public void CustomCharacterRow_EncodesExpectedPacket() =>
        Assert.Equal(
            new byte[] { 0xAB, 0x01, 0x03, 0x1F },
            Lis2Protocol.ProgramCharacterRow(1, 3, 0x1F));

    [Fact]
    public void WriteLine_EncodesGermanCharactersNatively()
    {
        var actual = Lis2Protocol.WriteLine(1, 0, "ÄÖÜ äöü ß");

        Assert.Equal(
            new byte[]
            {
                0xA1, 0x00, 0xA7,
                0x80, 0x86, 0x8A, 0x20,
                0xE1, 0x87, 0x8B, 0x20, 0xE2
            },
            actual);
    }

    [Fact]
    public void WriteLine_TruncatesAtDisplayBoundary()
    {
        var actual = Lis2Protocol.WriteLine(1, 18, "ABCD");
        Assert.Equal("AB", Encoding.ASCII.GetString(actual[3..]));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Fans_RejectInvalidPercentages(int value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Lis2Protocol.SetFans(value, 100, 100, 100));
    [Fact]
    public void WriteLine_EncodesCustomGlyphSlotAsRawCharacterByte()
    {
        var text = $"A{Lis2Protocol.CustomGlyph(3)}B";
        var actual = Lis2Protocol.WriteLine(1, 0, text);

        Assert.Equal(
            new byte[] { 0xA1, 0x00, 0xA7, 0x41, 0x03, 0x42 },
            actual);
    }

    [Fact]
    public void SafeDisplayText_PreservesNativeUnicodeAndCustomGlyphSentinels()
    {
        var glyph = Lis2Protocol.CustomGlyph(8);
        var actual = Lis2Protocol.ToSafeDisplayText($"ä°Ω←→{glyph}!");

        Assert.Equal($"ä°Ω←→{glyph}!", actual);
    }
    [Fact]
    public void WriteLine_EncodesDegreeSignAsNativeDisplayCharacter()
    {
        var actual = Lis2Protocol.WriteLine(1, 0, "23.5 °C");

        Assert.Equal(
            new byte[]
            {
                0xA1, 0x00, 0xA7,
                0x32, 0x33, 0x2E, 0x35, 0x20, 0xDF, 0x43
            },
            actual);
    }

    [Fact]
    public void SafeDisplayText_PreservesDegreeSignForPreview()
    {
        var actual = Lis2Protocol.ToSafeDisplayText("23.5 °C");

        Assert.Equal("23.5 °C", actual);
    }
    [Theory]
    [InlineData('Ä', 0x80)]
    [InlineData('Ö', 0x86)]
    [InlineData('Ü', 0x8A)]
    [InlineData('ä', 0xE1)]
    [InlineData('ö', 0x87)]
    [InlineData('ü', 0x8B)]
    [InlineData('ß', 0xE2)]
    [InlineData('°', 0xDF)]
    [InlineData('µ', 0xE4)]
    [InlineData('±', 0xB1)]
    [InlineData('£', 0x92)]
    [InlineData('×', 0x78)]
    [InlineData('÷', 0xFD)]
    [InlineData('←', 0x7F)]
    [InlineData('→', 0x7E)]
    [InlineData('Ω', 0xF4)]
    [InlineData('π', 0xF7)]
    [InlineData('Σ', 0xF6)]
    [InlineData('√', 0xE8)]
    [InlineData('∞', 0xF3)]
    public void UnicodeCharacter_EncodesToNativeDisplayByte(char character, int expected)
    {
        Assert.True(Lis2Protocol.TryEncodeDisplayCharacter(character, out var actual));
        Assert.Equal((byte)expected, actual);
    }

    [Fact]
    public void SafeAscii_StillTransliteratesGermanCharactersForIdentifiers()
    {
        Assert.Equal("AeOeUe aeoeue ss", Lis2Protocol.ToSafeAscii("ÄÖÜ äöü ß"));
    }

    [Fact]
    public void RawLine_WritesPayloadWithoutCharacterTranslation()
    {
        var actual = Lis2Protocol.WriteRawLine(
            2,
            0,
            new byte[] { 0x80, 0x86, 0x8A, 0xDF, 0xF4 });

        Assert.Equal(
            new byte[] { 0xA2, 0x00, 0xA7, 0x80, 0x86, 0x8A, 0xDF, 0xF4 },
            actual);
    }
    [Theory]
    [InlineData(new byte[] { 0xA0 }, "Clear/reset display")]
    [InlineData(new byte[] { 0xA5, 0x3A }, "Brightness 50%")]
    [InlineData(new byte[] { 0xAE, 0xF0, 25, 50, 75, 100 }, "Fans 25/50/75/100%")]
    [InlineData(new byte[] { 0xAB, 0x02, 0x03, 0x1F }, "Custom glyph slot 2, row 3, pixels 0x1F")]
    public void DescribeCommand_ReturnsSemanticMeaning(byte[] data, string expected)
    {
        Assert.Equal(expected, Lis2Protocol.DescribeCommand(data));
    }

    [Fact]
    public void DescribeCommand_DecodesDisplayText()
    {
        var data = Lis2Protocol.WriteLine(1, 4, "Dümmer");

        Assert.Equal(
            "Display line 1, column 4: \"Dümmer\"",
            Lis2Protocol.DescribeCommand(data));
    }
}
