using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using LIS2.WmpLegacy;

Console.WriteLine("LIS2 Windows Media Player Legacy Simulator");
Console.WriteLine($"Metadata pipe:      {WmpLegacyPipeServer.PipeName}");
Console.WriteLine($"Visualization pipe: {WmpLegacyVisualizationPipeServer.PipeName}");

using var metadataPipe = new NamedPipeClientStream(
    ".",
    WmpLegacyPipeServer.PipeName,
    PipeDirection.Out,
    PipeOptions.Asynchronous);

using var visualizationPipe = new NamedPipeClientStream(
    ".",
    WmpLegacyVisualizationPipeServer.PipeName,
    PipeDirection.Out,
    PipeOptions.Asynchronous);

await Task.WhenAll(
    metadataPipe.ConnectAsync(5000),
    visualizationPipe.ConnectAsync(5000));

await using var metadataWriter = new StreamWriter(
    metadataPipe,
    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
    bufferSize: 4096,
    leaveOpen: true)
{
    AutoFlush = true
};

await using var visualizationWriter = new StreamWriter(
    visualizationPipe,
    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
    bufferSize: 4096,
    leaveOpen: true)
{
    AutoFlush = true
};

var tracks = new[]
{
    new { Artist = "Dire Straits", Title = "Telegraph Road", Album = "Love over Gold", Duration = 845 },
    new { Artist = "Pink Floyd", Title = "Time", Album = "The Dark Side of the Moon", Duration = 421 },
    new { Artist = "Rush", Title = "Tom Sawyer", Album = "Moving Pictures", Duration = 276 }
};

for (var trackIndex = 0; trackIndex < tracks.Length; trackIndex++)
{
    var track = tracks[trackIndex];

    for (var elapsed = 0; elapsed <= 20; elapsed++)
    {
        await metadataWriter.WriteLineAsync(JsonSerializer.Serialize(new WmpLegacyMessage
        {
            Type = "snapshot",
            State = "playing",
            Artist = track.Artist,
            Title = track.Title,
            Album = track.Album,
            TrackNumber = trackIndex + 1,
            PlaylistCount = tracks.Length,
            ElapsedSeconds = elapsed,
            DurationSeconds = track.Duration
        }));

        var phase = elapsed * 0.55;
        var spectrum = Enumerable.Range(0, 20)
            .Select(index =>
            {
                var wave = (Math.Sin(phase + index * 0.48) + 1.0) / 2.0;
                var envelope = 1.0 - index / 28.0;
                return Math.Clamp(
                    (int)Math.Round(wave * envelope * 255),
                    0,
                    255);
            })
            .ToArray();

        await visualizationWriter.WriteLineAsync(
            JsonSerializer.Serialize(new WmpLegacyVisualizationMessage
            {
                Type = "visualization",
                VuLeft = Math.Clamp(
                    (int)Math.Round((Math.Sin(phase) + 1.0) * 110 + 30),
                    0,
                    255),
                VuRight = Math.Clamp(
                    (int)Math.Round((Math.Cos(phase * 0.87) + 1.0) * 105 + 35),
                    0,
                    255),
                Spectrum = spectrum
            }));

        Console.WriteLine(
            $"{trackIndex + 1}/{tracks.Length}: {track.Artist} - {track.Title} [{elapsed}s]");

        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    await metadataWriter.WriteLineAsync(JsonSerializer.Serialize(new WmpLegacyMessage
    {
        Type = "snapshot",
        State = "paused",
        Artist = track.Artist,
        Title = track.Title,
        Album = track.Album,
        TrackNumber = trackIndex + 1,
        PlaylistCount = tracks.Length,
        ElapsedSeconds = 20,
        DurationSeconds = track.Duration
    }));

    await visualizationWriter.WriteLineAsync(
        JsonSerializer.Serialize(new WmpLegacyVisualizationMessage
        {
            Type = "visualization",
            VuLeft = 0,
            VuRight = 0,
            Spectrum = new int[20]
        }));

    await Task.Delay(TimeSpan.FromMilliseconds(750));
}

await metadataWriter.WriteLineAsync(JsonSerializer.Serialize(new WmpLegacyMessage
{
    Type = "snapshot",
    State = "stopped"
}));

Console.WriteLine("Simulation finished.");
