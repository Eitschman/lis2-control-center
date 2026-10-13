using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageHomeAssistantTests
{
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
