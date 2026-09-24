namespace LIS2.Display;

public sealed record DisplayFrame(string Line1, string Line2)
{
    public const int Width = 20;

    public static DisplayFrame Create(string? line1, string? line2) =>
        new(Normalize(line1), Normalize(line2));

    public static string Normalize(string? value)
    {
        var text = LIS2.Core.Lis2Protocol.ToSafeDisplayText(value ?? string.Empty);
        if (text.Length > Width)
            text = text[..Width];

        return text.PadRight(Width);
    }
}
