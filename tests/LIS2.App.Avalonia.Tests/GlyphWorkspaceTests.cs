using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class GlyphWorkspaceTests
{
    [Fact]
    public void SpectrumReplacementUsesExactlyEightWpfBarLevels()
    {
        var workspace = new GlyphWorkspace();
        workspace.LoadExamples();
        Assert.False(workspace.IsSpectrumSet);
        workspace.LoadSpectrumSet();
        Assert.True(workspace.IsSpectrumSet);
        Assert.Equal(8, workspace.Glyphs.Count);
        for (var slot = 1; slot <= 8; slot++)
        {
            Assert.Equal("Spectrum" + slot, workspace.GetSlot(slot).Name);
            Assert.Equal(8, workspace.GetSlot(slot).Rows.Length);
            Assert.Equal(slot, workspace.GetSlot(slot).Rows.Count(row => row == 31));
        }
        var restored = new GlyphWorkspace();
        restored.ImportJson(workspace.ExportJson());
        Assert.True(restored.IsSpectrumSet);
    }

    [Fact]
    public async Task SpectrumGlyphsCanBeProgrammedToVirtualDevice()
    {
        var workspace = new GlyphWorkspace();
        workspace.LoadSpectrumSet();
        await using var session = new VirtualDeviceSession();
        await session.ConnectAsync();
        await session.ProgramAllGlyphsAsync(workspace.Glyphs);
        Assert.Equal(64, session.CommandCount);
        Assert.Equal(0, session.State.RejectedCommandCount);
        for (var index = 0; index < 8; index++)
            Assert.Equal(workspace.Glyphs[index].Rows, session.State.CustomCharacters[index]);
    }


    [Fact]
    public void JsonRoundtripPreservesGlyphs()
    {
        var source = new GlyphWorkspace();
        source.LoadExamples();
        var target = new GlyphWorkspace();
        target.ImportJson(source.ExportJson());
        Assert.Equal(source.GetSlot(4).Name, target.GetSlot(4).Name);
        Assert.Equal(source.GetSlot(4).Rows, target.GetSlot(4).Rows);
    }

    [Fact]
    public async Task ProgramAllWritesEightSlots()
    {
        var workspace = new GlyphWorkspace();
        workspace.LoadExamples();
        await using var session = new VirtualDeviceSession();
        await session.ConnectAsync();
        await session.ProgramAllGlyphsAsync(workspace.Glyphs);
        Assert.Equal(64, session.CommandCount);
        Assert.Equal(0, session.State.RejectedCommandCount);
        Assert.Equal(workspace.GetSlot(8).Rows, session.State.CustomCharacters[7]);
    }

    [Fact]
    public void ExamplesReuseOriginalGlyphs()
    {
        var workspace = new GlyphWorkspace();
        workspace.LoadExamples();
        Assert.Equal("Play", workspace.GetSlot(1).Name);
        Assert.Equal("BarFull", workspace.GetSlot(8).Name);
    }
}
