using System.Diagnostics;

namespace LIS2.App.Avalonia;

public sealed class LinuxMprisSource
{
    public static IReadOnlyDictionary<string, object?> Map(string state, string title, string artist)
        => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Connected"] = state.Length > 0,
            ["State"] = state,
            ["Title"] = title,
            ["Artist"] = artist
        };

    public static IReadOnlyDictionary<string, object?> Parse(string output)
    {
        var parts = output.TrimEnd('\r', '\n').Split('\t');
        if (parts.Length < 5) return Map("", "", "");
        var values = new Dictionary<string, object?>(Map(parts[0], parts[1], parts[2]), StringComparer.OrdinalIgnoreCase)
        {
            ["Album"] = parts[3], ["Duration"] = null
        };
        if (long.TryParse(parts[4], out var micros) && micros >= 0)
            values["Duration"] = TimeSpan.FromMicroseconds(micros).ToString(@"hh\:mm\:ss");
        return values;
    }

    public async Task<IReadOnlyDictionary<string, object?>> ReadAsync()
    {
        if (!OperatingSystem.IsLinux()) return Map("", "", "");
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "playerctl";
            process.StartInfo.ArgumentList.Add("metadata");
            process.StartInfo.ArgumentList.Add("--format");
            process.StartInfo.ArgumentList.Add("{{status}}\\t{{xesam:title}}\\t{{xesam:artist}}\\t{{xesam:album}}\\t{{mpris:length}}");
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            if (!process.Start()) return Map("", "", "");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var output = await process.StandardOutput.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            if (process.ExitCode == 0)
            {
                return Parse(output);
            }
        }
        catch (System.ComponentModel.Win32Exception) { }
        catch (InvalidOperationException) { }
        catch (OperationCanceledException) { }
        return Map("", "", "");
    }
}
