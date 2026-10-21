using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LIS2.App.Avalonia.Tests;

/// <summary>Lokaler HA-Protokollpartner mit künstlichem Token, ohne Zugriff auf Benutzerkonten.</summary>
internal sealed class HomeAssistantServer : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _timeout = new(TimeSpan.FromSeconds(25));
    private readonly List<WebSocket> _sockets = new();
    public string Url { get; }
    public HomeAssistantServer()
    {
        _listener.Start();
        Url = "http://127.0.0.1:" + ((IPEndPoint)_listener.LocalEndpoint).Port;
    }

    public async Task<WebSocket> AcceptAsync(bool authenticate = true)
    {
        var client = await _listener.AcceptTcpClientAsync(_timeout.Token);
        var stream = client.GetStream();
        var header = new StringBuilder();
        var buffer = new byte[1];
        while (!header.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
        {
            Assert.Equal(1, await stream.ReadAsync(buffer, _timeout.Token));
            header.Append((char)buffer[0]);
            Assert.True(header.Length < 8192);
        }
        Assert.StartsWith("GET /api/websocket HTTP/1.1", header.ToString());
        var key = header.ToString().Split("\r\n").Single(line => line.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase)).Split(':', 2)[1].Trim();
        var accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
        await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: " + accept + "\r\n\r\n"), _timeout.Token);
        var socket = WebSocket.CreateFromStream(stream, true, null, Timeout.InfiniteTimeSpan);
        _sockets.Add(socket);
        await SendAsync(socket, new { type = "auth_required", ha_version = "test" });
        using var auth = await ReadAsync(socket);
        Assert.Equal("auth", auth.RootElement.GetProperty("type").GetString());
        Assert.Equal("fixture-token", auth.RootElement.GetProperty("access_token").GetString());
        await SendAsync(socket, authenticate ? new { type = "auth_ok", ha_version = "test" } : (object)new { type = "auth_invalid", message = "Authentication failed." });
        return socket;
    }

    public async Task InitializeAsync(WebSocket socket)
    {
        using var states = await ReadAsync(socket);
        Assert.Equal("get_states", states.RootElement.GetProperty("type").GetString());
        using var subscribe = await ReadAsync(socket);
        Assert.Equal("subscribe_events", subscribe.RootElement.GetProperty("type").GetString());
        await SendAsync(socket, new { id = 2, type = "result", success = true, result = (object?)null });
        await SendAsync(socket, new { id = 1, type = "result", success = true, result = new[] { State("21") } });
    }

    public static object State(string value) => new { entity_id = "sensor.room", state = value, attributes = new { friendly_name = "Room", unit_of_measurement = "°C", battery = 87 } };
    public Task SendAsync(WebSocket socket, object message) => socket.SendAsync(new ArraySegment<byte>(JsonSerializer.SerializeToUtf8Bytes(message)), WebSocketMessageType.Text, true, _timeout.Token);
    public async Task<JsonDocument> ReadAsync(WebSocket socket)
    {
        using var stream = new MemoryStream();
        var buffer = new byte[4096];
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, _timeout.Token);
            Assert.Equal(WebSocketMessageType.Text, result.MessageType);
            stream.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);
        return JsonDocument.Parse(stream.ToArray());
    }

    public static async Task WaitAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(12);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "HA-Zustand wurde nicht rechtzeitig erreicht.");
            await Task.Delay(15);
        }
    }

    public ValueTask DisposeAsync()
    {
        _timeout.Cancel();
        foreach (var socket in _sockets) socket.Dispose();
        _listener.Stop();
        _timeout.Dispose();
        return ValueTask.CompletedTask;
    }
}
