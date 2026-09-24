namespace LIS2.App;

public sealed class CustomGlyphSettings
{
    public string Name { get; set; } = string.Empty;

    public byte[] Rows { get; set; } = new byte[8];

    public void EnsureDefaults()
    {
        if (Rows is null || Rows.Length != 8)
            Rows = new byte[8];

        for (var index = 0; index < Rows.Length; index++)
            Rows[index] = (byte)(Rows[index] & 0x1F);
    }
}
