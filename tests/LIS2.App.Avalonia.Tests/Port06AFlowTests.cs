using LIS2.App.Avalonia;
using LIS2.Core;
using LIS2.Display;
using LIS2.Winamp;

namespace LIS2.App.Avalonia.Tests;

public sealed class Port06AFlowTests
{
    [Theory]
    [InlineData(15)]
    [InlineData(255)]
    public async Task SnapshotPresetPreviewAndVirtualDeviceShareSpectrum(int maximum)
    {
        await using var sources = new PageSourceValues();
        var glyphs = new GlyphWorkspace();
        glyphs.LoadSpectrumSet();
        sources.SetGlyphNames(glyphs.Glyphs.Select(glyph => glyph.Name), glyphs.IsSpectrumSet);
        var sample = new WinampSnapshot(WinampPlaybackState.Playing, "Artist", "Title", null,
            1, 1, null, null, null, null, 255, 128, [0, 1, maximum / 2, maximum]);
        var telemetry = WinampValues.FromSnapshot(sample);
        sources.UpdateMedia("Winamp", telemetry);
        var values = sources.Snapshot(DateTimeOffset.Now);
        var line = new TemplateRenderer().Render(PagePresetCatalog.Get(9).Line2, values);
        Assert.Equal(20, line.Length);
        Assert.Equal(' ', line[0]);
        Assert.Equal(Lis2Protocol.CustomGlyph(8), line[3]);
        Assert.Equal('█', GlyphRendering.Preview(line, true)[3]);
        await using var device = new VirtualDeviceSession();
        await device.ConnectAsync();
        await device.ProgramAllGlyphsAsync(glyphs.Glyphs);
        await device.RenderBothAsync("Spectrum", line);
        Assert.Equal(line, device.State.Line2);
        Assert.Equal(0, device.State.RejectedCommandCount);
        sources.SetGlyphNames([], false);
        Assert.Equal(telemetry["Spectrum"], sources.Snapshot(DateTimeOffset.Now)["Winamp.SpectrumGlyphs"]);
        sources.UpdateMedia("Winamp", new Dictionary<string, object?> { ["Connected"] = false });
        Assert.Equal("", sources.Snapshot(DateTimeOffset.Now)["Winamp.SpectrumGlyphs"]);
    }

    [Fact]
    public void GlyphPreviewPreservesNormalCharactersAndIdentifiesCustomSlots()
    {
        var text = "Äß°" + Lis2Protocol.CustomGlyph(1) + Lis2Protocol.CustomGlyph(8);
        Assert.Equal("Äß°①⑧", GlyphRendering.Preview(text, false));
        Assert.Equal("Äß°▁█", GlyphRendering.Preview(text, true));
        Assert.Equal(new string(' ', 20), GlyphRendering.Spectrum([]));
    }
}
