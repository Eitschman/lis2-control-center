namespace LIS2.Winamp;

public static class WinampValues
{
    public static IReadOnlyDictionary<string, object?> FromSnapshot(WinampSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["State"] = snapshot.State.ToString(),
            ["Artist"] = snapshot.Artist,
            ["Title"] = snapshot.Title,
            ["Album"] = snapshot.Album,
            ["PlaylistPosition"] = snapshot.PlaylistPosition,
            ["PlaylistCount"] = snapshot.PlaylistCount,
            ["Elapsed"] = snapshot.Elapsed?.ToString(@"mm\:ss"),
            ["Duration"] = snapshot.Duration?.ToString(@"mm\:ss"),
            ["BitrateKbps"] = snapshot.BitrateKbps,
            ["SampleRateHz"] = snapshot.SampleRateHz,
            ["VuLeft"] = snapshot.VuLeft,
            ["VuRight"] = snapshot.VuRight,
            ["Vu"] = FormatVu(snapshot.VuLeft, snapshot.VuRight),
            ["Spectrum"] = FormatSpectrum(snapshot.Spectrum)
        };
    }

    public static string FormatVu(int? left, int? right)
    {
        if (left is null && right is null)
            return string.Empty;

        var l = Math.Clamp(left ?? 0, 0, 255);
        var r = Math.Clamp(right ?? 0, 0, 255);
        var leftBars = (int)Math.Round(l / 255.0 * 8);
        var rightBars = (int)Math.Round(r / 255.0 * 8);

        return $"L{new string('|', leftBars).PadRight(8)}  R{new string('|', rightBars).PadRight(8)}";
    }

    public static string FormatSpectrum(IReadOnlyList<int>? spectrum)
    {
        if (spectrum is null || spectrum.Count == 0)
            return string.Empty;

        const string levels = " .:-=+*#";
        var width = Math.Min(20, spectrum.Count);
        var chars = new char[width];

        for (var index = 0; index < width; index++)
        {
            var value = Math.Clamp(spectrum[index], 0, 255);
            var level = (int)Math.Round(value / 255.0 * (levels.Length - 1));
            chars[index] = levels[level];
        }

        return new string(chars);
    }
}
