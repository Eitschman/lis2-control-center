using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using LIS2.WmpLegacy;

Console.WriteLine("LIS2 Windows Media Player Legacy Simulator");
Console.WriteLine($"Connecting to pipe: {WmpLegacyPipeServer.PipeName}");

using var pipe = new NamedPipeClientStream(
    ".",
    WmpLegacyPipeServer.PipeName,
    PipeDirection.Out,
    PipeOptions.Asynchronous);

await pipe.ConnectAsync(5000);

await using var writer = new StreamWriter(
    pipe,
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

    for (var elapsed = 0; elapsed <= 20; elapsed += 5)
    {
        var message = new WmpLegacyMessage
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
        };

        await writer.WriteLineAsync(JsonSerializer.Serialize(message));

        Console.WriteLine(
            $"{trackIndex + 1}/{tracks.Length}: {track.Artist} - {track.Title} [{elapsed}s]");

        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    await writer.WriteLineAsync(JsonSerializer.Serialize(new WmpLegacyMessage
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

    await Task.Delay(TimeSpan.FromMilliseconds(750));
}

await writer.WriteLineAsync(JsonSerializer.Serialize(new WmpLegacyMessage
{
    Type = "snapshot",
    State = "stopped"
}));

Console.WriteLine("Simulation finished.");
