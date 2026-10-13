using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class GlyphWorkspaceTests
{
    [Fact]
    public void ExamplesReuseOriginalGlyphs()
    {
        var workspace = new GlyphWorkspace();
        workspace.LoadExamples();
        Assert.Equal("Play", workspace.GetSlot(1).Name);
        Assert.Equal("BarFull", workspace.GetSlot(8).Name);
    }
}
