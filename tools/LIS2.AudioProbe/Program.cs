using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using LIS2.WmpLegacy.Audio;

namespace LIS2.AudioProbe;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348))
        {
            Console.Error.WriteLine("Process loopback requires Windows build 20348 or later.");
            return 2;
        }
        if (args.Length != 3 || !int.TryParse(args[0], out var processId) || processId <= 0 ||
            !int.TryParse(args[1], out var seconds) || seconds is < 1 or > 3600)
        {
            Console.Error.WriteLine("Usage: LIS2.AudioProbe <wmplayer-pid> <seconds:1..3600> <new-report.jsonl>");
            return 2;
        }
        try
        {
            using var process = Process.GetProcessById(processId);
            if (!string.Equals(process.ProcessName, "wmplayer", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The selected process is not Windows Media Player Legacy.");
            using var output = new StreamWriter(new FileStream(args[2], FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
            using var capture = new ProcessLoopbackCapture();
            using var stop = new CancellationTokenSource();
            Console.CancelKeyPress += (_, cancel) => { cancel.Cancel = true; stop.Cancel(); };
            var analyzer = new SignalAnalyzer();
            var discontinuities = 0;
            capture.Start(processId);
            var elapsed = Stopwatch.StartNew();
            var next = TimeSpan.FromMilliseconds(100);
            Console.WriteLine($"Capturing only wmplayer PID {processId}; numeric telemetry only. Duration: {seconds}s.");
            while (!stop.IsCancellationRequested && elapsed.Elapsed < TimeSpan.FromSeconds(seconds))
            {
                if (process.HasExited) throw new InvalidOperationException("Target player exited; capture stopped.");
                capture.ReadAvailable((samples, discontinuity) =>
                {
                    analyzer.Add(samples);
                    if (discontinuity) discontinuities++;
                });
                if (elapsed.Elapsed < next) continue;
                var levels = analyzer.ReadAndResetPeaks();
                output.WriteLine(JsonSerializer.Serialize(new
                {
                    time = DateTimeOffset.UtcNow, processId, milliseconds = elapsed.ElapsedMilliseconds,
                    levels.Frames, levels.VuLeft, levels.VuRight, levels.Spectrum, discontinuities
                }));
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"{elapsed.Elapsed.TotalSeconds,7:F1}s L={levels.VuLeft,3} R={levels.VuRight,3} peak={levels.Spectrum.Max(),3} frames={levels.Frames}"));
                next = elapsed.Elapsed + TimeSpan.FromMilliseconds(100);
            }
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Capture failed: {error.Message} (0x{error.HResult:X8})");
            return 1;
        }
    }
}
