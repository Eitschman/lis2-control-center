namespace LIS2.Core;

internal static class VirtualLis2ProtocolInterpreter
{
    public static void Apply(VirtualLis2State state, ReadOnlySpan<byte> data)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (data.Length == 0)
        {
            Reject(state, "Empty LIS2 command.");
            return;
        }

        state.LastProtocolError = null;

        switch (data[0])
        {
            case 0xA0:
                if (data.Length != 1)
                {
                    Reject(state, "Clear command has an unexpected payload.");
                    return;
                }

                state.Line1 = new string(' ', 20);
                state.Line2 = new string(' ', 20);
                return;

            case 0xA1:
            case 0xA2:
                if (!ApplyText(state, data))
                    Reject(state, "Malformed LIS2 display-write command.");
                return;

            case 0xA5:
                if (data.Length < 2)
                {
                    Reject(state, "Brightness command is missing its level byte.");
                    return;
                }

                var brightness = data[1] switch
                {
                    0x38 => Lis2Brightness.Percent100,
                    0x39 => Lis2Brightness.Percent75,
                    0x3A => Lis2Brightness.Percent50,
                    0x3B => Lis2Brightness.Percent25,
                    _ => (Lis2Brightness?)null
                };

                if (brightness is null)
                {
                    Reject(state, $"Unknown LIS2 brightness byte 0x{data[1]:X2}.");
                    return;
                }

                state.Brightness = brightness.Value;
                return;

            case 0xAE:
                if (data.Length < 6 || data[1] != 0xF0)
                {
                    Reject(state, "Malformed LIS2 fan command.");
                    return;
                }

                state.Fan1 = data[2];
                state.Fan2 = data[3];
                state.Fan3 = data[4];
                state.Fan4 = data[5];
                return;

            case 0xAB:
                if (data.Length < 4)
                {
                    Reject(state, "Malformed LIS2 custom-character command.");
                    return;
                }

                var slot = data[1] - 1;
                var row = data[2];
                if (slot is < 0 or >= 8 || row >= 8)
                {
                    Reject(state, "Custom-character slot or row is outside the supported range.");
                    return;
                }

                state.CustomCharacters[slot][row] = data[3];
                return;

            default:
                Reject(state, $"Unknown LIS2 command 0x{data[0]:X2}.");
                return;
        }
    }

    private static bool ApplyText(VirtualLis2State state, ReadOnlySpan<byte> data)
    {
        if (data.Length < 3 || data[2] != 0xA7)
            return false;

        var column = data[1];
        if (column >= 20)
            return false;

        var current = (data[0] == 0xA1 ? state.Line1 : state.Line2).ToCharArray();
        var displayBytes = data[3..];

        for (var i = 0; i < displayBytes.Length && column + i < 20; i++)
            current[column + i] = Lis2Protocol.DecodeDisplayByte(displayBytes[i]);

        var updated = new string(current);

        if (data[0] == 0xA1)
            state.Line1 = updated;
        else
            state.Line2 = updated;

        return true;
    }

    private static void Reject(VirtualLis2State state, string message)
    {
        state.RejectedCommandCount++;
        state.LastProtocolError = message;
    }
}
