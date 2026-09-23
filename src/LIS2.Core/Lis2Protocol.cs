using System.Text;

namespace LIS2.Core;

public static class Lis2Protocol
{
    public static ReadOnlyMemory<byte> Clear => new byte[] { 0xA0 };

    public static byte[] WriteLine(int line, int column, string text)
    {
        if (line is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(line));

        if (column is < 0 or > 19)
            throw new ArgumentOutOfRangeException(nameof(column));

        ArgumentNullException.ThrowIfNull(text);

        var safeText = ToSafeAscii(text);
        var available = 20 - column;
        if (safeText.Length > available)
            safeText = safeText[..available];

        var bytes = Encoding.ASCII.GetBytes(safeText);
        var command = new byte[3 + bytes.Length];
        command[0] = line == 1 ? (byte)0xA1 : (byte)0xA2;
        command[1] = (byte)column;
        command[2] = 0xA7;
        bytes.CopyTo(command, 3);
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

    public static string ToSafeAscii(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value
            .Replace("ä", "ae", StringComparison.Ordinal)
            .Replace("ö", "oe", StringComparison.Ordinal)
            .Replace("ü", "ue", StringComparison.Ordinal)
            .Replace("Ä", "Ae", StringComparison.Ordinal)
            .Replace("Ö", "Oe", StringComparison.Ordinal)
            .Replace("Ü", "Ue", StringComparison.Ordinal)
            .Replace("ß", "ss", StringComparison.Ordinal)
            .Select(c => c is >= ' ' and <= '~' ? c : '?')
            .Aggregate(new StringBuilder(), (builder, c) => builder.Append(c))
            .ToString();
    }

    private static byte Percent(int value)
    {
        if (value is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(value), "Fan output must be between 0 and 100 percent.");

        return (byte)value;
    }
}
