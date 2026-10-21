using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageHomeAssistantTests
{
    [Fact]
    public async Task AliasesExposeStateUnitAttributesAndDisappearWhenDisabled()
    {
        await using var server = new HomeAssistantServer();
        await using var sources = new PageSourceValues();
        sources.SetHomeAssistantAliases(new Dictionary<string, string> { ["sensor.room"] = "Room Temp" });
        await sources.StartAsync();
        await sources.ConfigureHomeAssistantAsync(server.Url, "fixture-token");
        var socket = await server.AcceptAsync();
        await server.InitializeAsync(socket);
        await HomeAssistantServer.WaitAsync(() => sources.HomeAssistant.Values.Count > 0);
        var snapshot = sources.Snapshot(DateTimeOffset.Now);
        Assert.Equal("21", snapshot["HA.Room_Temp"]);
        Assert.Equal("°C", snapshot["HA.Room_Temp.Unit"]);
        Assert.Equal(87L, snapshot["HA.Room_Temp.attribute.battery"]);
        Assert.Equal("21", snapshot["HA.sensor.room"]);
        Assert.DoesNotContain(snapshot.Values, value => Equals(value, "fixture-token"));
        await sources.ConfigureHomeAssistantAsync(null, null);
        Assert.DoesNotContain(sources.Snapshot(DateTimeOffset.Now).Keys, key => key.StartsWith("HA."));
    }

    [Fact]
    public async Task HomeAssistantIsUnconfiguredByDefaultAndDoesNotExposeTokens()
    {
        await using var sources = new PageSourceValues();
        await sources.StartAsync();
        Assert.False(sources.HomeAssistant.IsConfigured);
        Assert.False(sources.HomeAssistant.IsConnected);
        Assert.DoesNotContain(sources.Snapshot(DateTimeOffset.Now).Keys, key => key.StartsWith("HA.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DisconnectClearsConfigurationWithoutNetworkAccess()
    {
        await using var sources = new PageSourceValues();
        await sources.StartAsync();
        await sources.ConfigureHomeAssistantAsync(null, null);
        Assert.False(sources.HomeAssistant.IsConfigured);
        Assert.False(sources.HomeAssistant.IsConnected);
        Assert.True(sources.Snapshot(DateTimeOffset.Now).ContainsKey("Clock.Time"));
    }
}
