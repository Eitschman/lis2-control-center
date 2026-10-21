using LIS2.App.Avalonia;
using LIS2.Core;
using LIS2.Display;

namespace LIS2.App.Avalonia.Tests;

public sealed class DesktopIntegrationTests
{
    [Fact]
    public void AutostartRoundTripEscapesExecutableAndPreservesOtherEntries()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-startup-" + Guid.NewGuid().ToString("N"));
        try
        {
            var startup = new DesktopAutostart(directory);
            Assert.False(startup.IsEnabled);
            var root = OperatingSystem.IsWindows() ? "C:/opt" : "/opt";
            startup.SetEnabled(true, root + "/My App/a\"$`%\\lis2");
            Assert.True(startup.IsEnabled);
            var entry = File.ReadAllText(startup.EntryPath);
            Assert.Contains("Exec=\"" + root + "/My App/a\\\\\"\\\\$\\\\`%%\\\\\\\\lis2\" --minimized\n", entry);
            var other = Path.Combine(Path.GetDirectoryName(startup.EntryPath)!, "other.desktop");
            File.WriteAllText(other, "untouched");
            startup.SetEnabled(false, "");
            startup.SetEnabled(false, "");
            Assert.False(startup.IsEnabled);
            Assert.Equal("untouched", File.ReadAllText(other));
            startup.SetEnabled(true, root + "/dotnet", root + "/My App/LIS2.dll");
            Assert.Contains("Exec=\"" + root + "/dotnet\" \"" + root + "/My App/LIS2.dll\" --minimized", File.ReadAllText(startup.EntryPath));
            File.AppendAllText(startup.EntryPath, "Hidden=true\n");
            Assert.False(startup.IsEnabled);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("relative/path")]
    [InlineData("/opt/broken\nExec=evil")]
    public void InvalidAutostartNeverCreatesEntry(string executable)
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-startup-" + Guid.NewGuid().ToString("N"));
        var startup = new DesktopAutostart(directory);
        Assert.Throws<ArgumentException>(() => startup.SetEnabled(true, executable));
        Assert.Throws<ArgumentException>(() => startup.SetEnabled(true, Path.Combine(Path.GetTempPath(), "invalid=executable")));
        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public async Task SecondInstanceActivatesFirstAndLeaseCanBeReacquired()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-instance-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (var primary = DesktopInstance.TryAcquire(directory))
            {
                Assert.NotNull(primary);
                Assert.Null(DesktopInstance.TryAcquire(directory));
                var activations = 0;
                primary.Listen(() => Interlocked.Increment(ref activations));
                Assert.True(await DesktopInstance.ActivateAsync(directory));
                Assert.True(await DesktopInstance.ActivateAsync(directory));
                Assert.Equal(2, Volatile.Read(ref activations));
            }
            using var restarted = DesktopInstance.TryAcquire(directory);
            Assert.NotNull(restarted);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task MissingInstanceDoesNotClaimActivation()
    {
        Assert.False(await DesktopInstance.ActivateAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
    }

    [Fact]
    public async Task NamedGlyphsRenderAndRenamesRemoveOldKeysWithoutOverwritingNumericSlots()
    {
        await using var sources = new PageSourceValues();
        sources.SetGlyphNames(["Play", "Pause!", "1", "Play"]);
        var values = sources.Snapshot(DateTimeOffset.Now);
        Assert.Equal(Lis2Protocol.CustomGlyph(4).ToString(), values["Glyph.Play"]);
        Assert.Equal(Lis2Protocol.CustomGlyph(2).ToString(), values["Glyph.Pause"]);
        Assert.Equal(Lis2Protocol.CustomGlyph(1).ToString(), values["Glyph.1"]);
        Assert.Equal(Lis2Protocol.CustomGlyph(4).ToString(), new TemplateRenderer().Render("{Glyph.play}", values));
        sources.SetGlyphNames(["Renamed"]);
        values = sources.Snapshot(DateTimeOffset.Now);
        Assert.False(values.ContainsKey("Glyph.Play"));
        Assert.Equal(Lis2Protocol.CustomGlyph(1).ToString(), values["Glyph.Renamed"]);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("tr")]
    [InlineData("ru")]
    public async Task StaticHelpDocumentsActualClockAndWinampKeysWithoutConnectedProviders(string language)
    {
        await using var sources = new PageSourceValues();
        var clockHelp = HelpVariableCatalog.Get("Clock.", language).ToDictionary(entry => entry.Heading, entry => entry.Body);
        foreach (var key in sources.Snapshot(DateTimeOffset.Now).Keys.Where(key => key.StartsWith("Clock.")))
            Assert.False(string.IsNullOrWhiteSpace(clockHelp["{" + key + "}"]));
        var snapshot = new LIS2.Winamp.WinampSnapshot(LIS2.Winamp.WinampPlaybackState.Playing,
            "Artist", "Title", "Album", 1, 2, TimeSpan.Zero, TimeSpan.FromMinutes(3), 128, 44100, 20, 30, [1, 2]);
        var mediaHelp = HelpVariableCatalog.Get("Winamp.", language).ToDictionary(entry => entry.Heading, entry => entry.Body);
        foreach (var key in LIS2.Winamp.WinampValues.FromSnapshot(snapshot).Keys)
            Assert.False(string.IsNullOrWhiteSpace(mediaHelp["{Winamp." + key + "}"]));
        Assert.Equal(8, HelpVariableCatalog.Get("Media.", language).Count());
    }
}
