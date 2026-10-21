namespace LIS2.App.Avalonia.Tests;

public sealed class PortableHaTokenStoreTests
{
    [Fact]
    public void NativeFailureNeverSelectsLocalStorage()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-ha-" + Guid.NewGuid().ToString("N"));
        try
        {
            new LegacyHaTokenStore(directory).Save("previously-stored-token");
            var store = new PortableHaTokenStore(directory, (_, _) => throw new IOException("Keyring locked"));
            Assert.Throws<IOException>(() => store.Load());
            Assert.Throws<IOException>(() => store.Save("updated-token"));
            Assert.Throws<IOException>(store.Delete);
            Assert.True(File.Exists(Path.Combine(directory, "ha-token.bin")));
            Assert.Equal("previously-stored-token", new LegacyHaTokenStore(directory).Load());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void LocalStorageRequiresExplicitSelection()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-ha-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new PortableHaTokenStore(directory, (_, _) => throw new IOException("No keyring"));
            Assert.Throws<IOException>(() => store.Save("new-token"));
            Assert.False(Directory.Exists(directory));
            store.UseLocalStorage = true;
            store.Save("new-local-token");
            Assert.Equal("new-local-token", store.Load());
            Assert.True(File.Exists(Path.Combine(directory, "ha-token.bin")));
            Assert.DoesNotContain("new-local-token", System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(directory, "ha-token.bin"))));
            store.Delete();
            Assert.Null(store.Load());
            Assert.False(File.Exists(Path.Combine(directory, "ha-token.key")));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MigrationDeletesLocalFilesOnlyAfterVerification(bool verified)
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-ha-" + Guid.NewGuid().ToString("N"));
        try
        {
            new LegacyHaTokenStore(directory).Save("migration-token");
            string? native = null;
            var store = new PortableHaTokenStore(directory, (operation, token) =>
            {
                if (operation == "store" && verified) native = token;
                return operation == "lookup" ? native : null;
            });
            if (verified) Assert.Equal("migration-token", store.Load());
            else Assert.Throws<System.Security.Cryptography.CryptographicException>(() => store.Load());
            Assert.Equal(!verified, File.Exists(Path.Combine(directory, "ha-token.key")));
            Assert.Equal(!verified, File.Exists(Path.Combine(directory, "ha-token.bin")));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("ciphertext")]
    [InlineData("key")]
    [InlineData("missing-key")]
    [InlineData("permissions")]
    public void LocalCorruptionIsReportedAndFilesAreRetained(string corruption)
    {
        if (corruption == "permissions" && OperatingSystem.IsWindows()) return;
        var directory = Path.Combine(Path.GetTempPath(), "lis2-ha-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new PortableHaTokenStore(directory) { UseLocalStorage = true };
            store.Save("local-token");
            var key = Path.Combine(directory, "ha-token.key");
            var ciphertext = Path.Combine(directory, "ha-token.bin");
            if (OperatingSystem.IsLinux())
            {
                Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(key));
                Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(ciphertext));
            }
            if (corruption == "ciphertext") File.WriteAllBytes(ciphertext, [1, 2, 3]);
            if (corruption == "key") File.WriteAllBytes(key, [1, 2, 3]);
            if (corruption == "missing-key") File.Delete(key);
            if (corruption == "permissions") File.SetUnixFileMode(key, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.OtherRead);
            Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(() => store.Load());
            Assert.True(File.Exists(ciphertext));
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
