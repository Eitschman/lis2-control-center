using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using LIS2.Winamp;

Console.WriteLine("LIS2 Winamp Simulator");
Console.WriteLine($"Connecting to pipe: {WinampPipeServer.PipeName}");

using var pipe = new NamedPipeClientStream(
    ".",
    WinampPipeServer.PipeName,
    PipeDirection.Out,
    PipeOptions.Asynchronous);

await pipe.ConnectAsync(TimeSpan.FromSeconds(5));

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
    new { Artist = "Iron Maiden", Title = "The Trooper", Album = "Piece of Mind", Duration = 245 },
    new { Artist = "Metallica", Title = "Master of Puppets", Album = "Master of Puppets", Duration = 515 },
    new { Artist = "AC/DC", Title = "Thunderstruck", Album = "The Razors Edge", Duration = 292 }
};

for (var trackIndex = 0; trackIndex < tracks.Length; trackIndex++)
{
    var track = tracks[trackIndex];

    for (var elapsed = 0; elapsed <= 20; elapsed += 5)
    {
        var message = new WinampMessage
        {
            Type = "snapshot",
            State = "playing",
            Artist = track.Artist,
            Title = track.Title,
            Album = track.Album,
            PlaylistPosition = trackIndex + 1,
            PlaylistCount = tracks.Length,
            ElapsedSeconds = elapsed,
            DurationSeconds = track.Duration,
            BitrateKbps = 320,
            SampleRateHz = 44100
        };

        var json = JsonSerializer.Serialize(message);
        await writer.WriteLineAsync(json);

        Console.WriteLine(
            $"{trackIndex + 1}/{tracks.Length}: {track.Artist} - {track.Title} [{elapsed}s]");

        await Task.Delay(TimeSpan.FromSeconds(1));
    }
}

await writer.WriteLineAsync(JsonSerializer.Serialize(new WinampMessage
{
    Type = "snapshot",
    State = "stopped"
}));

Console.WriteLine("Simulation finished.");
