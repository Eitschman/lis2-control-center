using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace LIS2.WmpLegacy;

public sealed class WmpLegacyPipeServer : IAsyncDisposable
{
    public const string PipeName = "LIS2ControlCenter.WmpLegacy";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _pipeName;

    public WmpLegacyPipeServer(string pipeName = PipeName) => _pipeName = pipeName;

    private CancellationTokenSource? _cts;
    private Task? _serverTask;

    public event EventHandler<WmpLegacySnapshot>? SnapshotReceived;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_serverTask is not null)
            return Task.CompletedTask;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _serverTask = RunAsync(_cts.Token);
        return _serverTask.IsCompleted ? _serverTask : Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is null)
            return;

        await _cts.CancelAsync().ConfigureAwait(false);

        try
        {
            if (_serverTask is not null)
            {
                try { await _serverTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _serverTask = null;
        }
    }

    internal void ThrowIfFaulted()
    {
        if (_serverTask is { IsFaulted: true }) _serverTask.GetAwaiter().GetResult();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var pipe = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

            using var reader = new StreamReader(
                pipe,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 4096,
                leaveOpen: true);

            while (pipe.IsConnected && !cancellationToken.IsCancellationRequested)
            {
                string? line;
                try { line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false); }
                catch (IOException exception)
                {
                    // Ein beendeter Player darf den Listener für die nächste Sitzung nicht beenden.
                    System.Diagnostics.Trace.TraceWarning("Legacy pipe {0} disconnected: {1}", _pipeName, exception.Message);
                    break;
                }
                if (line is null)
                    break;

                TryHandleMessage(line);
            }
        }
    }

    private void TryHandleMessage(string json)
    {
        var message = DeserializeMessage(json);
        if (message is null)
            return;

        SnapshotReceived?.Invoke(this, ToSnapshot(message));
    }

    public static WmpLegacyMessage? DeserializeMessage(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<WmpLegacyMessage>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static WmpLegacySnapshot ToSnapshot(WmpLegacyMessage message) =>
        new(
            ParseState(message.State),
            message.Artist,
            message.Title,
            message.Album,
            message.TrackNumber,
            message.PlaylistCount,
            ToTimeSpan(message.ElapsedSeconds),
            ToTimeSpan(message.DurationSeconds),
            message.StreamName,
            message.StreamUrl,
            message.IsStream);

    private static WmpLegacyPlaybackState ParseState(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "playing" => WmpLegacyPlaybackState.Playing,
            "paused" => WmpLegacyPlaybackState.Paused,
            "stopped" => WmpLegacyPlaybackState.Stopped,
            _ => WmpLegacyPlaybackState.Unknown
        };

    private static TimeSpan? ToTimeSpan(double? seconds) =>
        seconds is null || seconds < 0
            ? null
            : TimeSpan.FromSeconds(seconds.Value);

    public async ValueTask DisposeAsync() =>
        await StopAsync().ConfigureAwait(false);
}
