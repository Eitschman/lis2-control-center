using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace LIS2.WmpLegacy;

public sealed class WmpLegacyVisualizationPipeServer : IAsyncDisposable
{
    public const string PipeName = "LIS2ControlCenter.WmpLegacy.Visualization";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private CancellationTokenSource? _cts;
    private Task? _serverTask;

    public event EventHandler<WmpLegacyVisualizationMessage>? VisualizationReceived;

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

                var message = DeserializeMessage(line);
                if (message is not null)
                    VisualizationReceived?.Invoke(this, message);
            }
        }
    }

    public static WmpLegacyVisualizationMessage? DeserializeMessage(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<WmpLegacyVisualizationMessage>(
                json,
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync() =>
        await StopAsync().ConfigureAwait(false);
}
