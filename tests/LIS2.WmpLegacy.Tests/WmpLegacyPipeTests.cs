using System.IO.Pipes;
using System.Text;
using LIS2.WmpLegacy;

namespace LIS2.WmpLegacy.Tests;

public sealed class WmpLegacyPipeTests
{
    [Fact]
    public async Task OccupiedPipeReportsStartupFailureAndStillDisposes()
    {
        if (!OperatingSystem.IsWindows()) return;
        var pipeName = "lis2-occupied-test-" + Guid.NewGuid().ToString("N");
        using var occupied = new NamedPipeServerStream(pipeName, PipeDirection.In, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var server = new WmpLegacyPipeServer(pipeName);
        await Assert.ThrowsAsync<IOException>(() => server.StartAsync());
        await Assert.ThrowsAsync<IOException>(() => server.StopAsync());
        await server.DisposeAsync();
    }

    [Fact]
    public async Task RealPipesMergeTelemetryIgnoreMalformedMessagesAndReconnect()
    {
        var metadataPipe = "lis2-metadata-test-" + Guid.NewGuid().ToString("N");
        var audioPipe = "lis2-audio-test-" + Guid.NewGuid().ToString("N");
        await using var source = new WmpLegacyDataSource(metadataPipeName: metadataPipe, visualizationPipeName: audioPipe);
        await source.StartAsync();
        await SendAsync(metadataPipe, "invalid json", "{\"state\":\"Playing\",\"title\":\"First track\",\"artist\":\"Artist\",\"elapsedSeconds\":65}");
        await WaitAsync(() => Equals(source.Values["Title"], "First track"));
        await SendAsync(audioPipe, "{broken", "{\"vuLeft\":192,\"vuRight\":128,\"spectrum\":[0,15,128,255]}");
        await WaitAsync(() => Equals(source.Values["VuLeft"], 192));
        Assert.Equal("First track", source.Values["Title"]);
        Assert.Equal("01:05", source.Values["Elapsed"]);
        Assert.Equal(true, source.Values["Connected"]);
        Assert.Equal(true, source.Values["VisualizationConnected"]);
        await SendAsync(metadataPipe, "{\"state\":\"Paused\",\"title\":\"Second track\"}");
        await WaitAsync(() => Equals(source.Values["Title"], "Second track"));
        Assert.Equal("Paused", source.Values["State"]);
        Assert.Equal(128, source.Values["VuRight"]);
        await source.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task StopCancelsConnectedReadersAndAllowsRestart()
    {
        var metadataPipe = "lis2-stop-metadata-" + Guid.NewGuid().ToString("N");
        var audioPipe = "lis2-stop-audio-" + Guid.NewGuid().ToString("N");
        await using var source = new WmpLegacyDataSource(metadataPipeName: metadataPipe, visualizationPipeName: audioPipe);
        await source.StartAsync();
        using (var metadata = new NamedPipeClientStream(".", metadataPipe, PipeDirection.Out, PipeOptions.Asynchronous))
        using (var audio = new NamedPipeClientStream(".", audioPipe, PipeDirection.Out, PipeOptions.Asynchronous))
        {
            await metadata.ConnectAsync(3000);
            await audio.ConnectAsync(3000);
            await source.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
        }
        await source.StartAsync();
        await SendAsync(metadataPipe, "{\"title\":\"Restarted\"}");
        await WaitAsync(() => Equals(source.Values["Title"], "Restarted"));
        await source.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ConcurrentMetadataAndAudioUpdatesRetainBothSnapshots()
    {
        await using var source = new WmpLegacyDataSource();
        for (var iteration = 0; iteration < 250; iteration++)
        {
            var title = "Track " + iteration;
            await Task.WhenAll(
                Task.Run(() => source.ApplySnapshot(new(WmpLegacyPlaybackState.Playing, "Artist", title, "Album", 1, 2, TimeSpan.Zero, TimeSpan.FromMinutes(4)))),
                Task.Run(() => source.ApplyVisualization(new() { VuLeft = iteration, VuRight = 128, Spectrum = [iteration] })));
            Assert.Equal(title, source.Values["Title"]);
            Assert.Equal(iteration, source.Values["VuLeft"]);
        }
    }

    private static async Task SendAsync(string pipeName, params string[] messages)
    {
        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(3000);
        await using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        foreach (var message in messages) await writer.WriteLineAsync(message);
    }

    private static async Task WaitAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition()) await Task.Delay(10, timeout.Token);
    }
}
