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
            var store = new PortableHaTokenStore(directory, "/missing-secret-tool-for-test");
            store.Save("previously-stored-token");
            Assert.Equal("previously-stored-token", store.Load());
            Assert.True(File.Exists(Path.Combine(directory, "ha-token.bin")));
            store.Save("updated-token");
            Assert.Equal("updated-token", store.Load());
            store.Delete();
            Assert.Null(store.Load());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void MissingSecretToolFallsBackToLocalEncryptedToken()
    {
        if (!OperatingSystem.IsLinux()) return;
        var directory = Path.Combine(Path.GetTempPath(), "lis2-ha-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new PortableHaTokenStore(directory, "/missing-secret-tool-for-test");
            store.Save("new-fallback-token");
            Assert.Equal("new-fallback-token", store.Load());
            Assert.True(File.Exists(Path.Combine(directory, "ha-token.bin")));
            store.Delete();
            Assert.Null(store.Load());
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
