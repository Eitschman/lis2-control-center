using System.Text;

namespace LIS2.Core;

internal static class VirtualLis2ProtocolInterpreter
{
    public static void Apply(VirtualLis2State state, ReadOnlySpan<byte> data)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (data.Length == 0)
            return;

        switch (data[0])
        {
            case 0xA0:
                state.Line1 = new string(' ', 20);
                state.Line2 = new string(' ', 20);
                break;

            case 0xA1:
            case 0xA2:
                ApplyText(state, data);
                break;

            case 0xA5 when data.Length >= 2:
                state.Brightness = data[1] switch
                {
                    0x38 => Lis2Brightness.Percent100,
                    0x39 => Lis2Brightness.Percent75,
                    0x3A => Lis2Brightness.Percent50,
                    0x3B => Lis2Brightness.Percent25,
                    _ => state.Brightness
                };
                break;

            case 0xAE when data.Length >= 6 && data[1] == 0xF0:
                state.Fan1 = data[2];
                state.Fan2 = data[3];
                state.Fan3 = data[4];
                state.Fan4 = data[5];
                break;

            case 0xAB when data.Length >= 4:
                var slot = data[1] - 1;
                var row = data[2];
                if (slot is >= 0 and < 8 && row < 8)
                    state.CustomCharacters[slot][row] = data[3];
                break;
        }
    }

    private static void ApplyText(VirtualLis2State state, ReadOnlySpan<byte> data)
    {
        if (data.Length < 3 || data[2] != 0xA7)
            return;

        var column = data[1];
        if (column >= 20)
            return;

        var current = (data[0] == 0xA1 ? state.Line1 : state.Line2).ToCharArray();
        var text = Encoding.ASCII.GetString(data[3..]);

        for (var i = 0; i < text.Length && column + i < 20; i++)
            current[column + i] = text[i];

        var updated = new string(current);

        if (data[0] == 0xA1)
            state.Line1 = updated;
        else
            state.Line2 = updated;
    }
}
