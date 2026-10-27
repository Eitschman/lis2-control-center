namespace LIS2.WmpLegacy.Tests;

public sealed class ProcessAudioTests
{
    [Fact]
    public async Task DirectCaptureRejectsVisualizationPipeAndRetainsCapturedLevels()
    {
        await using var source = new WmpLegacyDataSource(enableProcessAudio: true);
        source.ApplyVisualization(new() { VuLeft = 180, VuRight = 90, Spectrum = [180, 90] });
        source.ApplyVisualizationPipe(new() { VuLeft = 0, VuRight = 0, Spectrum = [0, 0] });
        Assert.Equal(180, source.Values["VuLeft"]);
        Assert.Equal(90, source.Values["VuRight"]);
        Assert.Equal(new[] { 180, 90 }, source.Values["SpectrumRaw"]);
        Assert.True(source.ProcessAudioEnabled);
    }

    [Fact]
    public async Task SimulatorModeStillAcceptsVisualizationPipe()
    {
        await using var source = new WmpLegacyDataSource();
        source.ApplyVisualizationPipe(new() { VuLeft = 32, VuRight = 64, Spectrum = [12] });
        Assert.Equal(32, source.Values["VuLeft"]);
        Assert.False(source.ProcessAudioEnabled);
    }

    [Fact]
    public async Task DirectCaptureWaitingForMetadataStopsRestartsAndDisposes()
    {
        var source = new WmpLegacyDataSource(metadataPipeName: "lis2-process-meta-" + Guid.NewGuid().ToString("N"),
            visualizationPipeName: "lis2-process-audio-" + Guid.NewGuid().ToString("N"), enableProcessAudio: true);
        try
        {
            await source.StartAsync();
            await source.StopAsync().WaitAsync(TimeSpan.FromSeconds(3));
            await source.StartAsync();
        }
        finally { await source.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(3)); }
    }
}
