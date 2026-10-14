namespace LIS2.App.Avalonia.Tests;

public sealed class PortableHaTokenStoreTests
{
    [Fact]
    public void TokenRoundTripsWithoutPlaintextAndCanBeDeleted()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-ha-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new PortableHaTokenStore(directory);
            Assert.Null(store.Load());
            store.Save("long-lived-token-example");
            Assert.Equal("long-lived-token-example", store.Load());
            Assert.DoesNotContain("long-lived-token-example", Convert.ToBase64String(File.ReadAllBytes(Path.Combine(directory, "ha-token.bin"))));
            store.Save("replacement-token");
            Assert.Equal("replacement-token", store.Load());
            store.Delete();
            Assert.Null(store.Load());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
