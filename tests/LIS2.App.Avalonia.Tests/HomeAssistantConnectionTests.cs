using LIS2.Sources;

namespace LIS2.App.Avalonia.Tests;

public sealed class HomeAssistantConnectionTests
{
    [Fact]
    public async Task InitialSnapshotEventsReconnectAndDeactivation()
    {
        await using var server = new HomeAssistantServer();
        await using var source = new HomeAssistantDataSource();
        source.Configure(server.Url, "fixture-token");
        await source.StartAsync();
        var first = await server.AcceptAsync();
        await server.InitializeAsync(first);
        await HomeAssistantServer.WaitAsync(() => source.Values.ContainsKey("sensor.room"));
        Assert.Equal(87L, source.Values["sensor.room.attribute.battery"]);
        await server.SendAsync(first, new { id = 2, type = "event", @event = new { event_type = "state_changed", data = new { entity_id = "sensor.room", new_state = HomeAssistantServer.State("24") } } });
        await HomeAssistantServer.WaitAsync(() => Equals(source.Values.GetValueOrDefault("sensor.room"), "24"));
        var received = source.LastReceivedAt;
        Assert.NotNull(received);
        Assert.Equal(source.LastUpdateAt, received);
        first.Abort();
        await HomeAssistantServer.WaitAsync(() => !source.IsConnected);
        Assert.Empty(source.Values);
        Assert.Empty(source.Entities);
        Assert.Equal(received, source.LastReceivedAt);
        var second = await server.AcceptAsync();
        await server.InitializeAsync(second);
        await HomeAssistantServer.WaitAsync(() => source.Values.ContainsKey("sensor.room"));
        Assert.Equal("21", source.Values["sensor.room"]);
        await source.StopAsync().WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Empty(source.Values);
        Assert.Empty(source.Entities);
        Assert.Null(source.HomeAssistantVersion);
        Assert.Null(source.LastUpdateAt);
        source.Configure(null, null);
        Assert.Null(source.LastReceivedAt);
        await source.StartAsync();
        Assert.False(source.IsConfigured);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task FailedStateOrSubscriptionRequestIsVisible(int request)
    {
        await using var server = new HomeAssistantServer();
        await using var source = new HomeAssistantDataSource();
        source.Configure(server.Url, "fixture-token");
        await source.StartAsync();
        var socket = await server.AcceptAsync();
        await server.InitializeAsync(socket);
        await HomeAssistantServer.WaitAsync(() => source.Values.Count > 0);
        // Der Zustand ist erst mit Changed veröffentlicht, nicht schon beim Setzen des Fehlertexts.
        var rejected = new TaskCompletionSource<(bool Connected, int ValueCount)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        source.Changed += (_, _) =>
        {
            if (source.LastError is not null)
                rejected.TrySetResult((source.IsConnected, source.Values.Count));
        };
        await server.SendAsync(socket, new { id = request, type = "result", success = false, error = new { code = "test", message = "Request rejected" } });
        var state = await rejected.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.False(state.Connected);
        Assert.Equal(0, state.ValueCount);
        Assert.NotNull(source.LastError);
        Assert.Empty(source.Values);
    }

    [Fact]
    public async Task InvalidTokenIsReported()
    {
        await using var server = new HomeAssistantServer();
        await using var source = new HomeAssistantDataSource();
        source.Configure(server.Url, "fixture-token");
        var probe = source.TestConnectionAsync();
        await server.AcceptAsync(false);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => probe);
        Assert.Equal("Authentication failed.", error.Message);
    }

    [Theory]
    [InlineData("ftp://localhost")]
    [InlineData("file:///test")]
    [InlineData("wrong")]
    public void UnsupportedUrlIsNotConfigured(string url)
    {
        var source = new HomeAssistantDataSource();
        source.Configure(url, "fixture-token");
        Assert.False(source.IsConfigured);
    }
}
