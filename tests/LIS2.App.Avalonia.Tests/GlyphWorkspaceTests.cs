using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class GlyphWorkspaceTests
{
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
