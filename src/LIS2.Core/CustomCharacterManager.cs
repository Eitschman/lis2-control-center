namespace LIS2.Core;

public sealed class CustomCharacterManager
{
    private readonly ILis2Device _device;
    private readonly byte[][] _programmed =
        Enumerable.Range(0, 8).Select(_ => new byte[8]).ToArray();
    private readonly bool[] _known = new bool[8];

    public CustomCharacterManager(ILis2Device device) =>
        _device = device ?? throw new ArgumentNullException(nameof(device));

    public async Task ProgramSlotAsync(
        int slot,
        ReadOnlyMemory<byte> rows,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        Validate(slot, rows);

        var index = slot - 1;
        if (!force && _known[index] && rows.Span.SequenceEqual(_programmed[index]))
            return;

        await _device.ProgramCharacterAsync(slot, rows, cancellationToken)
            .ConfigureAwait(false);

        rows.Span.CopyTo(_programmed[index]);
        _known[index] = true;
    }

    public async Task ProgramAllAsync(
        IReadOnlyList<byte[]> glyphs,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(glyphs);

        if (glyphs.Count != 8)
            throw new ArgumentException("Exactly eight LIS2 custom characters are required.", nameof(glyphs));

        for (var slot = 1; slot <= 8; slot++)
            await ProgramSlotAsync(slot, glyphs[slot - 1], force, cancellationToken)
                .ConfigureAwait(false);
    }

    public void ResetCache() => Array.Clear(_known);

    private static void Validate(int slot, ReadOnlyMemory<byte> rows)
    {
        if (slot is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(slot));

        if (rows.Length != 8)
            throw new ArgumentException("A custom LIS2 character must contain exactly eight rows.", nameof(rows));

        foreach (var row in rows.Span)
        {
            if (row > 0x1F)
                throw new ArgumentOutOfRangeException(nameof(rows), "Each custom-character row must fit in five bits.");
        }
    }
}
