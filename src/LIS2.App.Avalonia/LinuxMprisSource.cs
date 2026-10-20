using System.Diagnostics;

namespace LIS2.App.Avalonia;

public sealed class LinuxMprisSource(string executable = "playerctl")
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
        var parts = output.TrimEnd('\r', '\n').Split('|');
        if (parts.Length < 5) return Map("", "", "");
        var values = new Dictionary<string, object?>(Map(parts[0], parts[1], parts[2]), StringComparer.OrdinalIgnoreCase)
        {
            ["Album"] = parts[3], ["Duration"] = null
        };
        if (long.TryParse(parts[4], out var micros) && micros >= 0)
            values["Duration"] = TimeSpan.FromMicroseconds(micros).ToString(@"hh\:mm\:ss");
        return values;
    }

    public async Task<IReadOnlyDictionary<string, object?>> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsLinux()) return Map("", "", "");
        using var process = new Process();
        try
        {
            process.StartInfo.FileName = executable;
            process.StartInfo.ArgumentList.Add("metadata");
            process.StartInfo.ArgumentList.Add("--format");
            process.StartInfo.ArgumentList.Add("{{status}}|{{xesam:title}}|{{xesam:artist}}|{{xesam:album}}|{{mpris:length}}");
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            if (!process.Start()) throw new InvalidOperationException("Could not start playerctl.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
            var output = await process.StandardOutput.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            var error = await errorTask;
            if (process.ExitCode == 0)
            {
                return Parse(output);
            }
            if (error.Contains("No players found", StringComparison.OrdinalIgnoreCase)) return Map("", "", "");
            throw new InvalidOperationException("playerctl failed with exit code " + process.ExitCode + ".");
        }
        finally
        {
            // Ein abgebrochener Refresh darf keinen Kindprozess über den Shutdown hinaus behalten.
            try
            {
                if (process.Id > 0 && !process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync(CancellationToken.None);
                }
            }
            catch (InvalidOperationException) { /* Der Prozess wurde nicht gestartet oder ist bereits beendet. */ }
        }
    }
}
