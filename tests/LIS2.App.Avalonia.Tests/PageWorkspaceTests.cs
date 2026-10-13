using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageWorkspaceTests
{
    [Fact]
    public void CrudAndReorderPreservePageData()
    {
        var workspace = new PageWorkspace();
        var first = workspace.Add();
        first.Name = "Clock";
        first.Line1Template = "Now: {Clock.Time|fallback:--}";
        var second = workspace.Duplicate(0);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal("Clock (copy)", second.Name);
        Assert.Equal(1, workspace.Move(0, 1));
        Assert.Equal(second.Id, workspace.Get(0).Id);
        workspace.Delete(0);
        Assert.Single(workspace.Pages);
        Assert.Equal(first.Id, workspace.Get(0).Id);
    }

    [Fact]
    public void PreviewUsesExistingDisplayTemplateRenderer()
    {
        var workspace = new PageWorkspace();
        workspace.Add().Line1Template = "CPU {Cpu.Load|percent:0|fallback:--}";
        var frame = workspace.Preview(0, new Dictionary<string, object?> { ["Cpu.Load"] = 42 });
        Assert.Equal("CPU 42%".PadRight(20), frame.Line1);
    }

    [Fact]
    public void JsonRoundtripKeepsOriginalPageFields()
    {
        var source = new PageWorkspace();
        var page = source.Add();
        page.RefreshIntervalMilliseconds = 200;
        page.Line2OverflowMode = "Marquee";
        page.VisibilityExpression = "Winamp.State=Playing";
        var restored = new PageWorkspace();
        restored.ImportJson(source.ExportJson());
        Assert.Equal(200, restored.Get(0).RefreshIntervalMilliseconds);
        Assert.Equal("Marquee", restored.Get(0).Line2OverflowMode);
        Assert.Equal("Winamp.State=Playing", restored.Get(0).VisibilityExpression);
    }

    [Fact]
    public void VisibilityExpressionsUseSharedDisplaySemantics()
    {
        var evaluator = new LIS2.Display.VisibilityEvaluator();
        var values = new Dictionary<string, object?> { ["Winamp.State"] = "Playing" };
        Assert.True(evaluator.IsVisible("Winamp.State=Playing", values));
        Assert.False(evaluator.IsVisible("Winamp.State=Paused", values));
        Assert.False(LIS2.Display.VisibilityEvaluator.TryParse("invalid expression", out _, out _, out _));
    }

    [Fact]
    public void InvalidVisibilityImportDoesNotReplaceExistingPages()
    {
        var workspace = new PageWorkspace();
        workspace.Add();
        Assert.Throws<InvalidOperationException>(() =>
            workspace.ImportJson("[{\\"Id\\":\\"valid\\",\\"DurationSeconds\\":5,\\"RefreshIntervalMilliseconds\\":1000,\\"VisibilityExpression\\":\\"invalid\\"}]"));
        Assert.Single(workspace.Pages);
    }

    [Fact]
    public void InvalidImportDoesNotReplaceExistingPages()
    {
        var workspace = new PageWorkspace();
        workspace.Add();
        Assert.Throws<InvalidOperationException>(() => workspace.ImportJson("[{\"Id\":\"bad\",\"DurationSeconds\":0}]"));
        Assert.Single(workspace.Pages);
    }
}
