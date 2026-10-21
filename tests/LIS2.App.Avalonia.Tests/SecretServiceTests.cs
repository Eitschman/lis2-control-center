using System.Diagnostics;
using System.Text;

namespace LIS2.App.Avalonia.Tests;

public sealed class SecretServiceTests
{
    [Theory]
    [InlineData("exit 1", false)]
    [InlineData("exit 2", true)]
    [InlineData("printf locked >&2; exit 1", true)]
    [InlineData("head -c 131072 /dev/zero >&2; exit 2", true)]
    public void MissingEntryAndServiceFailuresAreDistinguished(string command, bool failure)
    {
        if (!OperatingSystem.IsLinux()) return;
        WithTool(command, store =>
        {
            if (failure) Assert.Throws<IOException>(() => store.Load());
            else Assert.Null(store.Load());
        });
    }

    [Fact]
    public void HungServiceIsKilledEvenWhenOutputNeverCloses()
    {
        if (!OperatingSystem.IsLinux()) return;
        WithTool("sleep 30", store =>
        {
            var timer = Stopwatch.StartNew();
            Assert.Throws<TimeoutException>(() => store.Load());
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(5));
        });
    }

    [Fact]
    public void MissingExecutableDoesNotCreateLocalFiles()
    {
        if (!OperatingSystem.IsLinux()) return;
        var directory = Path.Combine(Path.GetTempPath(), "lis2-secret-" + Guid.NewGuid().ToString("N"));
        var store = new PortableHaTokenStore(directory, "/missing-lis2-secret-tool");
        Assert.Throws<IOException>(() => store.Save("fixture-token"));
        Assert.Throws<IOException>(() => store.Load());
        Assert.Throws<IOException>(store.Delete);
        Assert.False(Directory.Exists(directory));
    }

    [PrivateKeyringFact]
    public void RealPrivateDesktopKeyringRoundTripsAndMigrates()
    {
        // Der Workflow stellt einen privaten D-Bus samt temporärem Schlüsselbund bereit.
        if (!OperatingSystem.IsLinux()) return;
        Assert.Equal("1", Environment.GetEnvironmentVariable("LIS2_TEST_PRIVATE_KEYRING"));
        var directory = Path.Combine(Path.GetTempPath(), "lis2-keyring-" + Guid.NewGuid().ToString("N"));
        try
        {
            var legacy = new LegacyHaTokenStore(directory);
            legacy.Save("fixture-token");
            var store = new PortableHaTokenStore(directory);
            Assert.Equal("fixture-token", store.Load());
            Assert.False(File.Exists(Path.Combine(directory, "ha-token.bin")));
            Assert.False(File.Exists(Path.Combine(directory, "ha-token.key")));
            store.Save("fixture-updated-token");
            Assert.Equal("fixture-updated-token", store.Load());
            store.Delete();
            Assert.Null(store.Load());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    private static void WithTool(string command, Action<PortableHaTokenStore> test)
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-secret-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var executable = Path.Combine(directory, "secret-tool-fixture");
        try
        {
            using (var stream = new FileStream(executable, new FileStreamOptions
            {
                Mode = FileMode.CreateNew, Access = FileAccess.Write,
                UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
            })) stream.Write(Encoding.UTF8.GetBytes("#!/bin/sh\n" + command + "\n"));
            test(new PortableHaTokenStore(directory, executable, TimeSpan.FromMilliseconds(300)));
        }
        finally { Directory.Delete(directory, true); }
    }
}

internal sealed class PrivateKeyringFactAttribute : FactAttribute
{
    public PrivateKeyringFactAttribute()
    {
        if (!OperatingSystem.IsLinux() || Environment.GetEnvironmentVariable("LIS2_TEST_PRIVATE_KEYRING") != "1")
            Skip = "Benötigt einen isolierten Linux-D-Bus mit Testschlüsselbund.";
    }
}
