using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class ApplicationUpdateTests
{
    [Theory]
    [InlineData("v0.1.18", "0.1.18")]
    [InlineData("1.10.2", "1.10.2")]
    public void StableVersionIsComparedNumerically(string input, string expected)
    {
        Assert.Equal(Version.Parse(expected), ApplicationUpdates.ParseVersion(input));
        Assert.True(ApplicationUpdates.ParseVersion("0.1.10") > ApplicationUpdates.ParseVersion("0.1.9"));
    }

    [Theory]
    [InlineData("v0.1.19-beta")]
    [InlineData("0.1")]
    [InlineData("../0.1.19")]
    [InlineData("0.1.19.0")]
    public void InvalidOrPrereleaseVersionsAreRejected(string value) =>
        Assert.Throws<InvalidDataException>(() => ApplicationUpdates.ParseVersion(value));

    [Theory]
    [InlineData(true, Architecture.X64, false, "LIS2-Control-Center-1.2.3-win-x64.zip")]
    [InlineData(false, Architecture.X64, true, "lis2-control-center_1.2.3_amd64.deb")]
    [InlineData(false, Architecture.Arm64, true, "lis2-control-center_1.2.3_arm64.deb")]
    [InlineData(false, Architecture.Arm64, false, "LIS2-Control-Center-1.2.3-linux-arm64.tar.gz")]
    public void PackageMatchesPlatformAndArchitecture(bool windows, Architecture architecture, bool debian, string expected) =>
        Assert.Equal(expected, ApplicationUpdates.AssetName(new Version(1, 2, 3), windows, architecture, debian));

    [Fact]
    public void UnsupportedArchitectureDoesNotUseAnotherPackage() =>
        Assert.Throws<PlatformNotSupportedException>(() => ApplicationUpdates.AssetName(new Version(1, 2, 3), true, Architecture.Arm64, false));

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, true, true)]
    public async Task MetadataMustBeStableAndUseOfficialAssetUrls(bool draft, bool prerelease, bool wrongUrl, bool rejected)
    {
        var name = ApplicationUpdates.AssetName(new Version(9, 9, 9), false, Architecture.Arm64, true);
        var json = JsonSerializer.Serialize(new { draft, prerelease, tag_name = "v9.9.9", body = "Release text", assets = new[]
        {
            new { name, browser_download_url = wrongUrl ? "https://example.org/file.deb" : Url(name) },
            new { name = "SHA256SUMS.txt", browser_download_url = Url("SHA256SUMS.txt") }
        }});
        using var client = new HttpClient(new Handler(_ => new StringContent(json)));
        var service = new ApplicationUpdates(client);
        if (rejected) await Assert.ThrowsAsync<InvalidDataException>(() => service.CheckAsync(false, Architecture.Arm64, true, default));
        else
        {
            var update = await service.CheckAsync(false, Architecture.Arm64, true, default);
            Assert.Equal("Release text", update.Notes);
            Assert.Equal(name, update.AssetName);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DownloadMustMatchTheReleaseChecksum(bool corrupt)
    {
        var name = "lis2-control-center_9.9.9_arm64.deb";
        var bytes = Encoding.UTF8.GetBytes("package fixture");
        var expected = Convert.ToHexString(SHA256.HashData(bytes));
        var checksums = expected + "  " + name + "\n";
        using var client = new HttpClient(new Handler(uri => uri.AbsolutePath.EndsWith("SHA256SUMS.txt", StringComparison.Ordinal)
            ? new StringContent(checksums) : new ByteArrayContent(corrupt ? [1, 2, 3] : bytes)));
        var service = new ApplicationUpdates(client);
        var update = new AvailableUpdate(new Version(9, 9, 9), name, "", new Uri(Url(name)), new Uri(Url("SHA256SUMS.txt")));
        if (corrupt) await Assert.ThrowsAsync<InvalidDataException>(() => service.DownloadAsync(update, null, default));
        else
        {
            var download = await service.DownloadAsync(update, null, default);
            try
            {
                Assert.Equal(bytes, await File.ReadAllBytesAsync(download.Path));
                await File.WriteAllTextAsync(download.Path, "damaged after download");
                await Assert.ThrowsAsync<InvalidDataException>(() => ApplicationUpdates.VerifyAsync(download.Path, download.Sha256, default));
            }
            finally { Directory.Delete(Path.GetDirectoryName(download.Path)!, true); }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingOrDuplicateChecksumsAreRejected(bool duplicate)
    {
        var entry = new string('a', 64) + "  package.deb\n";
        Assert.Throws<InvalidDataException>(() => ApplicationUpdates.ExpectedChecksum(duplicate ? entry + entry : entry, duplicate ? "package.deb" : "other.deb"));
    }

    [Fact]
    public async Task HttpErrorsAndCancellationDoNotBecomeAnUpdate()
    {
        using var failed = new HttpClient(new StatusHandler());
        await Assert.ThrowsAsync<HttpRequestException>(() => new ApplicationUpdates(failed).CheckAsync(false, Architecture.X64, true, default));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        using var client = new HttpClient(new Handler(_ => new StringContent("{}")));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ApplicationUpdates(client).CheckAsync(false, Architecture.X64, true, cancelled.Token));
    }

    [Theory]
    [InlineData("../escape.dll")]
    [InlineData("folder/../../escape.dll")]
    [InlineData("C:/escape.dll")]
    [InlineData("folder\\..\\escape.dll")]
    public void ArchiveCannotEscapeItsStage(string entry)
    {
        using var fixture = new InstallationFixture();
        var zip = Path.Combine(fixture.Root, "package.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        using (var writer = new StreamWriter(archive.CreateEntry(entry).Open())) writer.Write("bad");
        Assert.Throws<InvalidDataException>(() => UpdateInstallation.ExtractWindowsPackage(zip, fixture.Plan.Stage, new Version(9, 9, 9)));
        Assert.False(File.Exists(Path.Combine(fixture.Root, "escape.dll")));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ArchiveMustSupplyItsOwnFilesAndMatchingVersion(bool validArchitecture)
    {
        using var fixture = new InstallationFixture();
        var zip = Path.Combine(fixture.Root, "package.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        {
            foreach (var name in new[] { "LIS2.App.Avalonia.exe", "LIS2.App.Avalonia.dll", "LIS2.App.Avalonia.runtimeconfig.json", "LIS2.App.Avalonia.deps.json" })
            {
                using var writer = new StreamWriter(archive.CreateEntry(name).Open());
                if (name.EndsWith(".exe", StringComparison.Ordinal))
                {
                    var executable = new byte[88];
                    executable[0] = 0x4d; executable[1] = 0x5a; executable[60] = 64;
                    executable[64] = 0x50; executable[65] = 0x45;
                    executable[68] = validArchitecture ? (byte)0x64 : (byte)0x4c;
                    executable[69] = validArchitecture ? (byte)0x86 : (byte)0x01;
                    writer.BaseStream.Write(executable);
                }
                else writer.Write(name.EndsWith("deps.json", StringComparison.Ordinal) ? "{\"libraries\":{\"LIS2.App.Avalonia/9.9.9\":{}}}" : "fixture");
            }
        }
        if (!validArchitecture)
        {
            Assert.Throws<InvalidDataException>(() => UpdateInstallation.ExtractWindowsPackage(zip, fixture.Plan.Stage, new Version(9, 9, 9)));
            return;
        }
        UpdateInstallation.ExtractWindowsPackage(zip, fixture.Plan.Stage, new Version(9, 9, 9));
        Assert.Throws<InvalidDataException>(() => UpdateInstallation.ExtractWindowsPackage(zip, fixture.Plan.Stage, new Version(9, 9, 8)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ArchiveRejectsLinksAndDuplicateTargets(bool link)
    {
        using var fixture = new InstallationFixture();
        var zip = Path.Combine(fixture.Root, "unsafe.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("file.txt");
            if (link) entry.ExternalAttributes = unchecked((int)0xa1ff0000);
            else archive.CreateEntry("FILE.txt");
        }
        Assert.Throws<InvalidDataException>(() => UpdateInstallation.ExtractWindowsPackage(zip, fixture.Plan.Stage, new Version(9, 9, 9)));
        Assert.Equal("old", File.ReadAllText(Path.Combine(fixture.Plan.Target, "version.txt")));
    }

    [Fact]
    public async Task OversizedMetadataIsRejectedBeforeParsing()
    {
        using var client = new HttpClient(new Handler(_ => new StringContent(new string('a', 2 * 1024 * 1024 + 1))));
        await Assert.ThrowsAsync<InvalidDataException>(() => new ApplicationUpdates(client).CheckAsync(false, Architecture.X64, true, default));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RestartConfirmationControlsCommitOrRollback(bool healthy)
    {
        using var fixture = new InstallationFixture();
        var restart = new Func<string, Task<bool>>(path =>
        {
            Assert.Equal("new", File.ReadAllText(Path.Combine(path, "version.txt")));
            return Task.FromResult(healthy);
        });
        if (healthy)
        {
            await UpdateInstallation.ReplaceAsync(fixture.Plan, restart);
            Assert.Equal("new", File.ReadAllText(Path.Combine(fixture.Plan.Target, "version.txt")));
            Assert.Equal("old", File.ReadAllText(Path.Combine(fixture.Plan.Backup, "version.txt")));
        }
        else
        {
            await Assert.ThrowsAsync<IOException>(() => UpdateInstallation.ReplaceAsync(fixture.Plan, restart));
            Assert.Equal("old", File.ReadAllText(Path.Combine(fixture.Plan.Target, "version.txt")));
            Assert.Equal("new", File.ReadAllText(Path.Combine(fixture.Plan.Stage, "version.txt")));
        }
    }

    [Fact]
    public async Task InterruptedReplacementRestoresOriginalBeforeRestart()
    {
        using var fixture = new InstallationFixture();
        Directory.Delete(fixture.Plan.Stage, true);
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => UpdateInstallation.ReplaceAsync(fixture.Plan, _ => throw new Exception("must not start")));
        Assert.Equal("old", File.ReadAllText(Path.Combine(fixture.Plan.Target, "version.txt")));
    }

    [Fact]
    public async Task RestartFailureRestoresOriginal()
    {
        using var fixture = new InstallationFixture();
        await Assert.ThrowsAsync<IOException>(() => UpdateInstallation.ReplaceAsync(fixture.Plan, _ => throw new IOException("restart denied")));
        Assert.Equal("old", File.ReadAllText(Path.Combine(fixture.Plan.Target, "version.txt")));
    }

    [Fact]
    public async Task LockedWindowsInstallationRemainsUnchanged()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new InstallationFixture();
        using var locked = new FileStream(Path.Combine(fixture.Plan.Target, "version.txt"), FileMode.Open, FileAccess.Read, FileShare.Read);
        await Assert.ThrowsAsync<IOException>(() => UpdateInstallation.ReplaceAsync(fixture.Plan, _ => throw new Exception("must not restart")));
        Assert.True(Directory.Exists(fixture.Plan.Target));
        Assert.True(Directory.Exists(fixture.Plan.Stage));
        Assert.False(Directory.Exists(fixture.Plan.Backup));
    }

    [Theory]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("tr")]
    [InlineData("ru")]
    public void HelpAndUpdateActionsAreLocalized(string language)
    {
        foreach (var text in new[] { "Check for updates", "Open system installer", "Install and restart", UpdateText.Help })
            Assert.NotEqual(text, HelpReferenceLocalization.Translate(text, language));
    }

    private static string Url(string name) => ApplicationUpdates.Repository + "/releases/download/v9.9.9/" + name;
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task LinuxHandoffPassesLiteralPathAndReportsDesktopFailure(int exitCode)
    {
        if (!OperatingSystem.IsLinux()) return;
        using var fixture = new InstallationFixture();
        var package = Path.Combine(fixture.Root, "package with spaces.deb");
        var recorded = Path.Combine(fixture.Root, "opened.txt");
        await File.WriteAllTextAsync(package, "fixture");
        System.Diagnostics.ProcessStartInfo Open(string path)
        {
            var info = new System.Diagnostics.ProcessStartInfo("/bin/sh");
            foreach (var argument in new[] { "-c", "printf '%s' \"$1\" > \"$2\"; echo missing-handler >&2; exit \"$3\"", "test-opener", path, recorded, exitCode.ToString() })
                info.ArgumentList.Add(argument);
            return info;
        }
        if (exitCode == 0) await UpdateInstallation.OpenLinuxPackageAsync(package, default, Open);
        else
        {
            var error = await Assert.ThrowsAsync<IOException>(() => UpdateInstallation.OpenLinuxPackageAsync(package, default, Open));
            Assert.Contains("missing-handler", error.Message);
        }
        Assert.Equal(package, await File.ReadAllTextAsync(recorded));
    }

    private sealed class Handler(Func<Uri, HttpContent> content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content(request.RequestUri!) });
        }
    }
    private sealed class StatusHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
    }
    private sealed class InstallationFixture : IDisposable
    {
        internal string Root { get; } = Path.Combine(Path.GetTempPath(), "lis2-update-test-" + Guid.NewGuid().ToString("N"));
        internal WindowsUpdatePlan Plan { get; }
        internal InstallationFixture()
        {
            Plan = new(Path.Combine(Root, "installed"), Path.Combine(Root, "stage"), Path.Combine(Root, "previous"), Path.Combine(Root, "session"), 1, 0);
            foreach (var directory in new[] { Plan.Target, Plan.Stage, Plan.Session }) Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(Plan.Target, "version.txt"), "old");
            File.WriteAllText(Path.Combine(Plan.Stage, "version.txt"), "new");
        }
        public void Dispose() => Directory.Delete(Root, true);
    }
}
