using System.Text;

namespace LIS2.Core;

public static class Lis2Protocol
{
    public const char CustomGlyphBase = '\uE000';
    public const byte DegreeSymbolByte = 0xDF;

    // LCDproc's uPD16314 ROM-code-002 character map. Input indices are
    // ISO-8859-1 U+00A0..U+00FF and values are native display bytes.
    private static readonly byte[] Latin1ToDisplay =
    [
        0x20, 0x21, 0xEC, 0x92, 0xA4, 0x5C, 0x98, 0x8F,
        0x22, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
        0xDF, 0xB1, 0xB2, 0xB3, 0x27, 0xE4, 0xF7, 0xA5,
        0x2C, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0x3F,
        0x81, 0x81, 0x41, 0x41, 0x80, 0x82, 0x90, 0x99,
        0x45, 0x45, 0x45, 0x45, 0x49, 0x49, 0x49, 0x49,
        0x44, 0xEE, 0x4F, 0x4F, 0x4F, 0x4F, 0x86, 0x78,
        0x88, 0x55, 0x55, 0x55, 0x8A, 0x59, 0xF0, 0xE2,
        0x61, 0x83, 0x61, 0x61, 0xE1, 0x84, 0x91, 0x99,
        0x65, 0x65, 0x65, 0x65, 0x69, 0x69, 0x69, 0x69,
        0x6F, 0xEE, 0x6F, 0x6F, 0x6F, 0x6F, 0x87, 0xFD,
        0x89, 0x75, 0x75, 0x75, 0x8B, 0x79, 0xF0, 0xFF
    ];

    private static readonly Dictionary<char, byte> ExtraUnicodeToDisplay =
        new()
        {
            ['←'] = 0x7F,
            ['→'] = 0x7E,
            ['Ω'] = 0xF4,
            ['ω'] = 0xF4,
            ['π'] = 0xF7,
            ['Σ'] = 0xF6,
            ['σ'] = 0xE5,
            ['α'] = 0xE0,
            ['β'] = 0xE2,
            ['μ'] = 0xE4,
            ['√'] = 0xE8,
            ['∞'] = 0xF3
        };

    private static readonly Dictionary<byte, char> PreferredDisplayToUnicode =
        BuildPreferredDisplayToUnicode();

    public static ReadOnlyMemory<byte> Clear => new byte[] { 0xA0 };

    public static byte[] WriteLine(int line, int column, string text)
    {
        if (line is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(line));

        if (column is < 0 or > 19)
            throw new ArgumentOutOfRangeException(nameof(column));

        ArgumentNullException.ThrowIfNull(text);

        var safeText = ToSafeDisplayText(text);
        var available = 20 - column;
        if (safeText.Length > available)
            safeText = safeText[..available];

        return WriteRawLine(line, column, EncodeDisplayText(safeText));
    }

    public static byte[] WriteRawLine(
        int line,
        int column,
        ReadOnlySpan<byte> displayBytes)
    {
        if (line is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(line));

        if (column is < 0 or > 19)
            throw new ArgumentOutOfRangeException(nameof(column));

        var available = 20 - column;
        var length = Math.Min(displayBytes.Length, available);
        var command = new byte[3 + length];
        command[0] = line == 1 ? (byte)0xA1 : (byte)0xA2;
        command[1] = (byte)column;
        command[2] = 0xA7;
        displayBytes[..length].CopyTo(command.AsSpan(3));
        return command;
    }

    public static byte[] SetBrightness(Lis2Brightness brightness) =>
        new byte[]
        {
            0xA5,
            brightness switch
            {
                Lis2Brightness.Percent100 => 0x38,
                Lis2Brightness.Percent75 => 0x39,
                Lis2Brightness.Percent50 => 0x3A,
                Lis2Brightness.Percent25 => 0x3B,
                _ => throw new ArgumentOutOfRangeException(nameof(brightness))
            }
        };

    public static byte[] SetFans(int fan1, int fan2, int fan3, int fan4) =>
        new byte[] { 0xAE, 0xF0, Percent(fan1), Percent(fan2), Percent(fan3), Percent(fan4) };

    public static byte[] ProgramCharacterRow(int character, int row, int pixels)
    {
        if (character is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(character));

        if (row is < 0 or > 7)
            throw new ArgumentOutOfRangeException(nameof(row));

        if (pixels is < 0 or > 0x1F)
            throw new ArgumentOutOfRangeException(nameof(pixels));

        return new byte[] { 0xAB, (byte)character, (byte)row, (byte)pixels };
    }

    public static char CustomGlyph(int slot)
    {
        if (slot is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(slot));

        return (char)(CustomGlyphBase + slot - 1);
    }

    public static bool TryGetCustomGlyphSlot(char value, out int slot)
    {
        slot = value - CustomGlyphBase + 1;
        return slot is >= 1 and <= 8;
    }

    public static bool TryEncodeDisplayCharacter(char character, out byte value)
    {
        if (TryGetCustomGlyphSlot(character, out var slot))
        {
            value = (byte)slot;
            return true;
        }

        if (character is >= ' ' and <= '}')
        {
            value = (byte)character;
            return true;
        }

        // The ROM uses 0x7E/0x7F for arrows, so Unicode arrows are preferred
        // over ASCII '~' for those two cells.
        if (character == '~')
        {
            value = 0x8E;
            return true;
        }

        if (character is >= '\u00A0' and <= '\u00FF')
        {
            value = Latin1ToDisplay[character - '\u00A0'];
            return true;
        }

        return ExtraUnicodeToDisplay.TryGetValue(character, out value);
    }

    public static char DecodeDisplayByte(byte value)
    {
        if (value is >= 1 and <= 8)
            return CustomGlyph(value);

        if (PreferredDisplayToUnicode.TryGetValue(value, out var mapped))
            return mapped;

        if (value is >= 0x20 and <= 0x7D)
            return (char)value;

        return '?';
    }

    public static string ToSafeDisplayText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value
            .Select(character =>
                TryEncodeDisplayCharacter(character, out _)
                    ? character
                    : '?')
            .Aggregate(
                new StringBuilder(),
                (builder, character) => builder.Append(character))
            .ToString();
    }

    public static string ToSafeAscii(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var expanded = value
            .Replace("ä", "ae", StringComparison.Ordinal)
            .Replace("ö", "oe", StringComparison.Ordinal)
            .Replace("ü", "ue", StringComparison.Ordinal)
            .Replace("Ä", "Ae", StringComparison.Ordinal)
            .Replace("Ö", "Oe", StringComparison.Ordinal)
            .Replace("Ü", "Ue", StringComparison.Ordinal)
            .Replace("ß", "ss", StringComparison.Ordinal);

        return expanded
            .Select(character =>
                TryGetCustomGlyphSlot(character, out _)
                    ? '?'
                    : character is >= ' ' and <= '~'
                        ? character
                        : '?')
            .Aggregate(
                new StringBuilder(),
                (builder, character) => builder.Append(character))
            .ToString();
    }

    public static byte[] EncodeDisplayText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var safe = ToSafeDisplayText(value);
        var bytes = new byte[safe.Length];

        for (var index = 0; index < safe.Length; index++)
        {
            bytes[index] = TryEncodeDisplayCharacter(safe[index], out var encoded)
                ? encoded
                : (byte)'?';
        }

        return bytes;
    }

    private static Dictionary<byte, char> BuildPreferredDisplayToUnicode()
    {
        var result = new Dictionary<byte, char>();

        for (var value = 0x20; value <= 0x7D; value++)
            result[(byte)value] = (char)value;

        // Prefer human-readable Unicode forms for the cells we explicitly use.
        result[0x7E] = '→';
        result[0x7F] = '←';
        result[0x80] = 'Ä';
        result[0x86] = 'Ö';
        result[0x87] = 'ö';
        result[0x8A] = 'Ü';
        result[0x8B] = 'ü';
        result[0xDF] = '°';
        result[0xE1] = 'ä';
        result[0xE2] = 'ß';
        result[0xE4] = 'µ';
        result[0xF3] = '∞';
        result[0xF4] = 'Ω';
        result[0xF6] = 'Σ';
        result[0xF7] = 'π';
        result[0xFD] = '÷';

        return result;
    }

    private static byte Percent(int value)
    {
        if (value is < 0 or > 100)
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Fan output must be between 0 and 100 percent.");

        return (byte)value;
    }
}
