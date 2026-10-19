namespace LIS2.App.Avalonia.Tests;

public sealed class PortableHaTokenStoreTests
{
    [Fact]
    public void LegacyTokenSurvivesWhenNativeStoreIsUnavailable()
    {
        if (!OperatingSystem.IsLinux()) return;
        var directory = Path.Combine(Path.GetTempPath(), "lis2-ha-" + Guid.NewGuid().ToString("N"));
        try
        {
            var legacy = new LegacyHaTokenStore(directory);
            legacy.Save("previously-stored-token");
            var store = new PortableHaTokenStore(directory, "/missing-secret-tool-for-test");
            Assert.Equal("previously-stored-token", store.Load());
            Assert.True(File.Exists(Path.Combine(directory, "ha-token.bin")));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void WindowsDpapiTokenRoundTrips()
    {
        if (!OperatingSystem.IsWindows()) return;
        var directory = Path.Combine(Path.GetTempPath(), "lis2-ha-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new PortableHaTokenStore(directory);
            store.Save("windows-token");
            Assert.Equal("windows-token", store.Load());
            Assert.False(File.Exists(Path.Combine(directory, "ha-token.bin")));
            store.Delete();
            Assert.Null(store.Load());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
