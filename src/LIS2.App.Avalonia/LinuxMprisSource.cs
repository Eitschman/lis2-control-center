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

    public async Task<IReadOnlyDictionary<string, object?>> ReadAsync()
    {
        if (!OperatingSystem.IsLinux()) return Map("", "", "");
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "playerctl";
            process.StartInfo.ArgumentList.Add("metadata");
            process.StartInfo.ArgumentList.Add("--format");
            process.StartInfo.ArgumentList.Add("{{status}}|{{title}}|{{artist}}");
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (process.ExitCode == 0)
            {
                var parts = output.Trim().Split('|');
                if (parts.Length >= 3) return Map(parts[0], parts[1], parts[2]);
            }
        }
        catch (System.ComponentModel.Win32Exception) { }
        return Map("", "", "");
    }
}
