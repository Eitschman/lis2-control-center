using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;

namespace LIS2.App.Avalonia;

public sealed class LinuxMprisSource(string executable = "playerctl",
    Func<IReadOnlyList<string>, CancellationToken, Task<PlayerctlResult>>? command = null)
{
    // XML-Escaping erhält Trennzeichen und Zeilenumbrüche in echten Titeln und Interpreten.
    internal const string MetadataFormat = "<media><state>{{status}}</state><title>{{markup_escape(xesam:title)}}</title>" +
        "<artist>{{markup_escape(xesam:artist)}}</artist><album>{{markup_escape(xesam:album)}}</album>" +
        "<elapsed>{{position}}</elapsed><duration>{{mpris:length}}</duration></media>";

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
        var media = XElement.Parse(output);
        if (media.Name != "media" || media.Element("state")?.Value is not ("Playing" or "Paused" or "Stopped"))
            throw new FormatException("Invalid playerctl metadata.");
        string Read(string name) => media.Element(name)?.Value ?? throw new FormatException("Invalid playerctl metadata.");
        return new Dictionary<string, object?>(Map(Read("state"), Read("title"), Read("artist")), StringComparer.OrdinalIgnoreCase)
        {
            ["Album"] = Read("album"), ["Elapsed"] = FormatTime(Read("elapsed")), ["Duration"] = FormatTime(Read("duration"))
        };
    }

    private static string? FormatTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var micros) ||
            micros > TimeSpan.MaxValue.Ticks / 10)
            throw new FormatException("Invalid playerctl time.");
        var time = TimeSpan.FromTicks(micros * 10);
        return $"{(long)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
    }

    public async Task<MprisSnapshot> ReadSnapshotAsync(string? selectedPlayer, CancellationToken cancellationToken = default)
    {
        var listing = await RunAsync(["--list-all"], cancellationToken);
        var players = listing.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal).ToArray();
        // Eine gespeicherte Auswahl bleibt bei Abwesenheit getrennt und wechselt nicht zu einem anderen Player.
        var player = string.IsNullOrEmpty(selectedPlayer) ? players.FirstOrDefault() : selectedPlayer;
        if (player is null || !players.Contains(player, StringComparer.Ordinal))
            return new(players, Map("", "", ""));
        var result = await RunAsync(["--player=" + player, "metadata", "--format", MetadataFormat], cancellationToken);
        var values = string.IsNullOrWhiteSpace(result.Output) ? Map("", "", "") : Parse(result.Output);
        return new(players, new Dictionary<string, object?>(values) { ["Player"] = player });
    }

    public async Task<IReadOnlyDictionary<string, object?>> ReadAsync(CancellationToken cancellationToken = default)
        => (await ReadSnapshotAsync(null, cancellationToken)).Values;

    private async Task<PlayerctlResult> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var result = await (command is null ? ExecuteAsync(executable, arguments, cancellationToken, TimeSpan.FromSeconds(3)) : command(arguments, cancellationToken));
        if (result.ExitCode == 0) return result;
        if (result.Error.Trim() is "No players found" or "No player could handle this command") return new(0, "", "");
        throw new InvalidOperationException($"playerctl failed ({result.ExitCode}): {result.Error.Trim()}");
    }

    internal static async Task<PlayerctlResult> ExecuteAsync(string executable, IReadOnlyList<string> arguments,
        CancellationToken cancellationToken, TimeSpan requestTimeout, IReadOnlyDictionary<string, string>? environment = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var process = new Process();
        try
        {
            process.StartInfo.FileName = executable;
            foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            // Fehlertexte für die Unterscheidung zwischen fehlenden Playern und Providerfehlern stabil halten.
            process.StartInfo.Environment["LC_ALL"] = "C";
            if (environment is not null)
                foreach (var pair in environment) process.StartInfo.Environment[pair.Key] = pair.Value;
            if (!process.Start()) throw new InvalidOperationException("Could not start playerctl.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(requestTimeout);
            var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
            var output = await process.StandardOutput.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            var error = await errorTask;
            return new(process.ExitCode, output, error);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Media request timed out.");
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

public sealed record PlayerctlResult(int ExitCode, string Output, string Error);
public sealed record MprisSnapshot(IReadOnlyList<string> Players, IReadOnlyDictionary<string, object?> Values);
