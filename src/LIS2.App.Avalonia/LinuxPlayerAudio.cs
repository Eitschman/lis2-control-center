using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using LIS2.WmpLegacy;
using LIS2.WmpLegacy.Audio;

namespace LIS2.App.Avalonia;

/// <summary>Erfasst ausschließlich den PipeWire-Wiedergabestream des gewählten MPRIS-Prozesses.</summary>
internal sealed class LinuxPlayerAudio(IReadOnlyDictionary<string, string>? environment = null) : IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly CancellationTokenSource _lifetime = new();
    private Task _worker = Task.CompletedTask;
    private Task _capture = Task.CompletedTask;
    private CancellationTokenSource? _captureCancellation;
    private string? _player;
    private long _selection, _generation;
    private LinuxAudioTarget? _target;
    private IReadOnlyDictionary<string, object?> _values = Empty("Waiting for playback");
    private DateTimeOffset? _lastFrame;
    private bool? _explicitRaw;
    private string _identityDetails = "";

    internal string IdentityDetails { get { lock (_sync) return _identityDetails; } }

    // Prozessbestand und FFT bleiben außerhalb des UI-Threads, auch bei einem Start aus Avalonia.
    internal void Start() => _worker = Task.Run(() => RunAsync(_lifetime.Token));

    /// <summary>Ein Quellenwechsel leert Pegel sofort und entwertet verspätete Prozessantworten.</summary>
    internal void SelectPlayer(string? player, bool playing)
    {
        player = playing ? player : null;
        lock (_sync)
        {
            if (_player == player) return;
            _player = player;
            _selection++;
            _generation++;
            _captureCancellation?.Cancel();
            _lastFrame = null;
            _identityDetails = "";
            _values = Empty(player is null ? "Waiting for playback" : "Resolving player audio");
        }
    }

    internal IReadOnlyDictionary<string, object?> Values
    {
        get
        {
            lock (_sync)
            {
                if (_lastFrame is not null && DateTimeOffset.UtcNow - _lastFrame > TimeSpan.FromMilliseconds(750))
                    return Empty("No audio data from the selected player");
                return new Dictionary<string, object?>(_values);
            }
        }
    }

    private async Task RunAsync(CancellationToken cancellation)
    {
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                string? player;
                long selection;
                lock (_sync) { player = _player; selection = _selection; }
                try
                {
                    if (player is null) await StopCaptureAsync();
                    else
                    {
                        var owner = await CommandAsync("dbus-send", ["--session", "--print-reply", "--dest=org.freedesktop.DBus",
                            "/org/freedesktop/DBus", "org.freedesktop.DBus.GetConnectionUnixProcessID",
                            "string:org.mpris.MediaPlayer2." + player], cancellation);
                        var processId = ParseProcessId(owner);
                        var graph = await CommandAsync("pw-dump", [], cancellation);
                        var sandbox = LinuxSandboxSelection.Read(processId);
                        lock (_sync)
                        {
                            if (selection == _selection)
                                _identityDetails = "D-Bus PID: " + processId.ToString(CultureInfo.InvariantCulture) +
                                    (sandbox is null ? "; host process" : "; Flatpak: " + sandbox.Identity.ApplicationId +
                                    "; instance: " + sandbox.Identity.InstanceId + "; mapped processes: " + sandbox.Processes.Count);
                        }
                        var target = FindTarget(graph, processId, IsProcessOrChild, sandbox);
                        // Ältere pw-record-Versionen geben auf stdout direkt PCM aus; neuere verlangen --raw.
                        _explicitRaw ??= (await CommandAsync("pw-record", ["--help"], cancellation)).Contains("--raw", StringComparison.Ordinal);
                        bool changed;
                        lock (_sync)
                        {
                            if (selection != _selection) continue;
                            changed = target != _target || _capture.IsCompleted || _captureCancellation?.IsCancellationRequested == true;
                        }
                        if (changed)
                        {
                            await StopCaptureAsync();
                            lock (_sync)
                            {
                                if (selection != _selection) continue;
                                _target = target;
                                _lastFrame = null;
                                _values = Empty("Connecting player audio");
                                _captureCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                                _capture = CaptureAsync(target, ++_generation, _captureCancellation.Token);
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { break; }
                catch (Exception exception)
                {
                    await StopCaptureAsync();
                    lock (_sync)
                    {
                        if (selection == _selection)
                        {
                            _lastFrame = null;
                            _values = Empty(ErrorText(exception));
                        }
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(1), cancellation);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        finally { await StopCaptureAsync(); }
    }

    private async Task<string> CommandAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellation)
    {
        PlayerctlResult result;
        try { result = await LinuxMprisSource.ExecuteAsync(executable, arguments, cancellation, TimeSpan.FromSeconds(3), environment); }
        catch (TimeoutException exception) { throw new TimeoutException(executable + ": " + exception.Message, exception); }
        if (result.ExitCode != 0) throw new InvalidOperationException(executable + ": " + result.Error.Trim());
        return result.Output;
    }

    internal static int ParseProcessId(string output)
    {
        var match = Regex.Match(output, @"(?m)^\s*uint32 ([0-9]+)\s*$", RegexOptions.CultureInvariant);
        return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var processId) && processId > 0
            ? processId : throw new InvalidOperationException("Cannot identify the selected player process");
    }

    /// <summary>Nur Wiedergabeknoten mit bestätigter Prozesszuordnung sind zulässig, niemals Geräte oder Mikrofone.</summary>
    internal static LinuxAudioTarget FindTarget(string json, int playerProcessId, Func<int, int, bool>? belongsToPlayer = null,
        LinuxSandboxSelection? sandbox = null)
    {
        using var document = JsonDocument.Parse(json);
        var clients = new Dictionary<int, JsonElement>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.GetProperty("type").GetString() != "PipeWire:Interface:Client" ||
                !item.TryGetProperty("info", out var info) || !info.TryGetProperty("props", out var props)) continue;
            clients[item.GetProperty("id").GetInt32()] = props;
        }
        var targets = new List<(LinuxAudioTarget Target, bool Running)>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.GetProperty("type").GetString() != "PipeWire:Interface:Node" ||
                !item.TryGetProperty("info", out var info) || !info.TryGetProperty("props", out var props) ||
                ReadText(props, "media.class") != "Stream/Output/Audio") continue;
            var processId = ReadInt(props, "application.process.id");
            var client = ReadInt(props, "client.id") is { } clientId && clients.TryGetValue(clientId, out var clientProps)
                ? clientProps : default;
            processId ??= ReadInt(client, "application.process.id");
            if (sandbox is not null)
                processId = sandbox.Resolve(processId, ReadInt(client, "pipewire.sec.pid"),
                    ReadText(client, "pipewire.access.portal.app_id"), ReadText(client, "pipewire.access.portal.instance_id"));
            else
            {
                processId ??= ReadInt(client, "pipewire.sec.pid");
                if (processId is not null && !(belongsToPlayer?.Invoke(processId.Value, playerProcessId) ?? processId == playerProcessId)) continue;
            }
            if (processId is null) continue;
            var serial = ReadText(props, "object.serial");
            if (!ulong.TryParse(serial, NumberStyles.None, CultureInfo.InvariantCulture, out _)) continue;
            targets.Add((new(serial, processId.Value, ReadText(props, "node.name")),
                info.TryGetProperty("state", out var state) && state.GetString() == "running"));
        }
        var running = targets.Where(target => target.Running).ToArray();
        var candidates = running.Length > 0 ? running : targets.ToArray();
        return candidates.Length switch
        {
            1 => candidates[0].Target,
            0 => throw new InvalidOperationException("No audio stream for the selected player"),
            _ => throw new InvalidOperationException("Multiple audio streams for the selected player")
        };
    }

    private static string ReadText(JsonElement props, string key) => props.ValueKind == JsonValueKind.Object &&
        props.TryGetProperty(key, out var value) ? value.ToString() : "";
    private static int? ReadInt(JsonElement props, string key) => int.TryParse(ReadText(props, key), NumberStyles.None,
        CultureInfo.InvariantCulture, out var number) && number > 0 ? number : null;

    private static bool IsProcessOrChild(int processId, int parentId)
    {
        for (var depth = 0; depth < 32 && processId > 1; depth++)
        {
            if (processId == parentId) return true;
            try
            {
                var stat = File.ReadAllText("/proc/" + processId.ToString(CultureInfo.InvariantCulture) + "/stat");
                var fields = stat[(stat.LastIndexOf(')') + 2)..].Split(' ');
                processId = int.Parse(fields[1], CultureInfo.InvariantCulture);
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
        return false;
    }

    internal static IReadOnlyList<string> CaptureArguments(LinuxAudioTarget target, bool explicitRaw = false)
    {
        var arguments = new List<string> { "--format=s16", "--rate=44100", "--channels=2", "--latency=50ms", "--target=" + target.Serial,
            "--properties={ stream.capture.sink=true node.dont-fallback=true node.dont-reconnect=true node.dont-move=true node.name=lis2-player-audio media.name=\"LIS2 player audio\" }" };
        if (explicitRaw) arguments.Add("--raw");
        arguments.Add("-");
        return arguments;
    }

    /// <summary>PCM bleibt im Arbeitsspeicher. Unvollständige Stereo-Frames werden bis zum nächsten Read erhalten.</summary>
    private async Task CaptureAsync(LinuxAudioTarget target, long generation, CancellationToken cancellation)
    {
        using var process = new Process { StartInfo = new("pw-record")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true
        } };
        foreach (var argument in CaptureArguments(target, _explicitRaw is true)) process.StartInfo.ArgumentList.Add(argument);
        process.StartInfo.Environment["LC_ALL"] = "C";
        if (environment is not null)
            foreach (var pair in environment) process.StartInfo.Environment[pair.Key] = pair.Value;
        try
        {
            if (!process.Start()) throw new InvalidOperationException("Could not start PipeWire capture");
            var errors = process.StandardError.ReadToEndAsync(cancellation);
            var analyzer = new SignalAnalyzer();
            var buffer = new byte[8192];
            var remaining = 0;
            var publication = Stopwatch.StartNew();
            while (true)
            {
                var count = await process.StandardOutput.BaseStream.ReadAsync(buffer.AsMemory(remaining), cancellation);
                if (count == 0) break;
                var total = remaining + count;
                var complete = total - total % 4;
                var samples = new short[complete / 2];
                for (var index = 0; index < samples.Length; index++)
                    samples[index] = BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(index * 2, 2));
                analyzer.Add(samples);
                remaining = total - complete;
                buffer.AsSpan(complete, remaining).CopyTo(buffer);
                if (publication.Elapsed < TimeSpan.FromMilliseconds(100)) continue;
                var levels = analyzer.ReadAndResetPeaks();
                lock (_sync)
                {
                    if (generation != _generation || cancellation.IsCancellationRequested) return;
                    _values = Map(levels, target);
                    _lastFrame = DateTimeOffset.UtcNow;
                }
                publication.Restart();
            }
            await process.WaitForExitAsync(cancellation);
            throw new InvalidOperationException("PipeWire capture stopped: " + (await errors).Trim());
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception exception)
        {
            lock (_sync)
            {
                if (generation == _generation)
                {
                    _lastFrame = null;
                    _values = Empty(ErrorText(exception));
                }
            }
        }
        finally
        {
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

    internal static IReadOnlyDictionary<string, object?> Map(AudioLevels levels, LinuxAudioTarget target) =>
        new Dictionary<string, object?>
        {
            ["AudioConnected"] = true, ["AudioStatus"] = "Connected", ["AudioTarget"] = target.Name,
            ["AudioProcessId"] = target.ProcessId, ["AudioNodeSerial"] = target.Serial,
            ["VuLeft"] = levels.VuLeft, ["VuRight"] = levels.VuRight,
            ["Vu"] = WmpLegacyValues.FormatVu(levels.VuLeft, levels.VuRight),
            ["SpectrumRaw"] = levels.Spectrum, ["Spectrum"] = WmpLegacyValues.FormatSpectrum(levels.Spectrum),
            ["SpectrumPeak"] = levels.Spectrum.Max()
        };

    private static IReadOnlyDictionary<string, object?> Empty(string status) => new Dictionary<string, object?>
    {
        ["AudioConnected"] = false, ["AudioStatus"] = status, ["AudioTarget"] = "", ["Vu"] = "", ["Spectrum"] = ""
    };

    private static string ErrorText(Exception exception) => exception is System.ComponentModel.Win32Exception { NativeErrorCode: 2 }
        ? "Linux audio tools are missing: install pipewire-bin and dbus-bin" : exception.Message;

    private async Task StopCaptureAsync()
    {
        lock (_sync) { _generation++; _captureCancellation?.Cancel(); }
        await _capture;
        lock (_sync)
        {
            _captureCancellation?.Dispose();
            _captureCancellation = null;
            _target = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel();
        lock (_sync) { _generation++; _captureCancellation?.Cancel(); _values = Empty("Disconnected"); _lastFrame = null; }
        await _worker;
        _lifetime.Dispose();
    }
}

internal sealed record LinuxAudioTarget(string Serial, int ProcessId, string Name);
