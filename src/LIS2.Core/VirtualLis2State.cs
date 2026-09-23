namespace LIS2.Core;

public sealed class VirtualLis2State
{
    public string Line1 { get; internal set; } = new(' ', 20);
    public string Line2 { get; internal set; } = new(' ', 20);
    public Lis2Brightness Brightness { get; internal set; } = Lis2Brightness.Percent100;
    public int Fan1 { get; internal set; } = 100;
    public int Fan2 { get; internal set; } = 100;
    public int Fan3 { get; internal set; } = 100;
    public int Fan4 { get; internal set; } = 100;
    public byte[][] CustomCharacters { get; } =
        Enumerable.Range(0, 8).Select(_ => new byte[8]).ToArray();
}
