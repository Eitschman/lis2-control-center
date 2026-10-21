using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;

namespace LIS2.App.Avalonia;

/// <summary>Eine Instanz pro Benutzerprofil hält das Gerät; Folgestarts aktivieren ihr Fenster.</summary>
internal sealed class DesktopInstance : IDisposable
{
    private readonly FileStream _lease;
    private readonly CancellationTokenSource _stopping = new();
    private readonly string _pipeName;
    private Task? _listener;

    private DesktopInstance(FileStream lease, string pipeName) { _lease = lease; _pipeName = pipeName; }

    internal static DesktopInstance? TryAcquire(string profileDirectory)
    {
        Directory.CreateDirectory(profileDirectory);
        FileStream lease;
        try { lease = new FileStream(Path.Combine(profileDirectory, "desktop.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException) { return null; }
        return new DesktopInstance(lease, PipeName(profileDirectory));
    }

    private static string PipeName(string directory) => "LIS2-" + Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(directory))))[..32];

    internal void Listen(Action activate)
    {
        if (_listener is not null) throw new InvalidOperationException("Activation listener already started.");
        _listener = ListenAsync(activate);
    }

    private async Task ListenAsync(Action activate)
    {
        try
        {
            while (!_stopping.IsCancellationRequested)
            {
                using var server = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(_stopping.Token).ConfigureAwait(false);
                using var request = CancellationTokenSource.CreateLinkedTokenSource(_stopping.Token);
                request.CancelAfter(TimeSpan.FromSeconds(3));
                var signal = new byte[1];
                try
                {
                    if (await server.ReadAsync(signal, request.Token).ConfigureAwait(false) == 1 && signal[0] == 1)
                    {
                        activate();
                        await server.WriteAsync(new byte[] { 1 }, request.Token).ConfigureAwait(false);
                    }
                }
                catch (IOException) { /* Ein abgebrochener Folgestart beendet die Hauptinstanz nicht. */ }
                catch (OperationCanceledException) when (!_stopping.IsCancellationRequested) { }
            }
        }
        catch (OperationCanceledException) when (_stopping.IsCancellationRequested) { }
    }

    internal static async Task<bool> ActivateAsync(string directory)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName(directory), PipeDirection.InOut,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await client.ConnectAsync(timeout.Token).ConfigureAwait(false);
            await client.WriteAsync(new byte[] { 1 }, timeout.Token).ConfigureAwait(false);
            var reply = new byte[1];
            return await client.ReadAsync(reply, timeout.Token).ConfigureAwait(false) == 1 && reply[0] == 1;
        }
        catch (OperationCanceledException) { return false; }
        catch (IOException) { return false; }
    }

    public void Dispose()
    {
        _stopping.Cancel();
        try { _listener?.GetAwaiter().GetResult(); }
        finally { _lease.Dispose(); _stopping.Dispose(); }
    }
}
