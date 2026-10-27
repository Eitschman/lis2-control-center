using System.Diagnostics;
using System.Runtime.Versioning;

namespace LIS2.WmpLegacy.Audio;

/// <summary>Bindet die direkte Audioquelle an genau einen Legacy-Player der aktuellen Sitzung.</summary>
internal sealed class ProcessAudioWorker(
    Func<bool> metadataConnected,
    Action<WmpLegacyVisualizationMessage> publish,
    Action<string?> reportError)
{
    public Task RunAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348))
                reportError("Direct audio capture requires Windows build 20348 or later.");
            else RunWindows(cancellationToken);
        }, CancellationToken.None);
    }

    [SupportedOSPlatform("windows10.0.20348")]
    private void RunWindows(CancellationToken cancellationToken)
    {
        using var current = Process.GetCurrentProcess();
        var session = current.SessionId;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!metadataConnected())
                {
                    cancellationToken.WaitHandle.WaitOne(250);
                    continue;
                }
                using var player = FindPlayer(session);
                if (player is null)
                {
                    cancellationToken.WaitHandle.WaitOne(250);
                    continue;
                }
                using var capture = new ProcessLoopbackCapture();
                var analyzer = new SignalAnalyzer();
                capture.Start(player.Id);
                reportError(null);
                var elapsed = Stopwatch.StartNew();
                var next = TimeSpan.FromMilliseconds(100);
                while (!cancellationToken.IsCancellationRequested && !player.HasExited && metadataConnected())
                {
                    capture.ReadAvailable((samples, _) => analyzer.Add(samples));
                    if (elapsed.Elapsed < next) continue;
                    var levels = analyzer.ReadAndResetPeaks();
                    if (levels.Frames > 0)
                        publish(new WmpLegacyVisualizationMessage
                        {
                            VuLeft = levels.VuLeft, VuRight = levels.VuRight, Spectrum = levels.Spectrum
                        });
                    next = elapsed.Elapsed + TimeSpan.FromMilliseconds(100);
                }
            }
            catch (Exception error)
            {
                reportError(error.GetBaseException().Message);
                // Wiederverbindung derselben ausgewählten Quelle; kein Wechsel zur Visualisierungs-Pipe.
                cancellationToken.WaitHandle.WaitOne(1000);
            }
        }
    }

    internal static Process? FindPlayer(int session)
    {
        Process? match = null;
        var candidates = Process.GetProcessesByName("wmplayer");
        try
        {
            foreach (var candidate in candidates)
            {
                if (!candidate.HasExited && candidate.SessionId == session)
                {
                    if (match is not null)
                        throw new InvalidOperationException("Several Legacy players are running in this session; close additional players for direct audio capture.");
                    match = candidate;
                }
            }
            return match;
        }
        catch { match?.Dispose(); throw; }
        finally
        {
            foreach (var candidate in candidates)
                if (!ReferenceEquals(candidate, match)) candidate.Dispose();
        }
    }
}
