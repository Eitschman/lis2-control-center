using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace LIS2.Winamp;

public sealed class WinampPipeServer : IAsyncDisposable
{
    public const string PipeName = "LIS2ControlCenter.Winamp";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private CancellationTokenSource? _cts;
    private Task? _serverTask;

    public event EventHandler<WinampSnapshot>? SnapshotReceived;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_serverTask is not null)
            return Task.CompletedTask;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _serverTask = RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is null)
            return;

        await _cts.CancelAsync().ConfigureAwait(false);

        if (_serverTask is not null)
        {
            try
            {
                await _serverTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _cts.Dispose();
        _cts = null;
        _serverTask = null;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var pipe = new NamedPipeServerStream(
                PipeName,
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
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
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

    public static WinampMessage? DeserializeMessage(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<WinampMessage>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static WinampSnapshot ToSnapshot(WinampMessage message) =>
        new(
            ParseState(message.State),
            message.Artist,
            message.Title,
            message.Album,
            message.PlaylistPosition,
            message.PlaylistCount,
            ToTimeSpan(message.ElapsedSeconds),
            ToTimeSpan(message.DurationSeconds),
            message.BitrateKbps,
            message.SampleRateHz,
            message.VuLeft,
            message.VuRight,
            message.Spectrum);

    private static WinampPlaybackState ParseState(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "playing" => WinampPlaybackState.Playing,
            "paused" => WinampPlaybackState.Paused,
            "stopped" => WinampPlaybackState.Stopped,
            _ => WinampPlaybackState.Unknown
        };

    private static TimeSpan? ToTimeSpan(double? seconds) =>
        seconds is null || seconds < 0
            ? null
            : TimeSpan.FromSeconds(seconds.Value);

    public async ValueTask DisposeAsync() =>
        await StopAsync().ConfigureAwait(false);
}
