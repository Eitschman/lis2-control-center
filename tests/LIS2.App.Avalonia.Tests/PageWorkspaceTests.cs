using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageWorkspaceTests
{
    [Fact]
    public void ImportedPageOrderAndEnabledFlagsArePreserved()
    {
        var source = new PageWorkspace();
        source.Add().Name = "First";
        var disabled = source.Add();
        disabled.Name = "Second";
        disabled.Enabled = false;
        var target = new PageWorkspace();
        target.Add().Name = "Old";
        target.ImportJson(source.ExportJson());
        Assert.Equal(new[] { "First", "Second" }, target.Pages.Select(p => p.Name));
        Assert.True(target.Get(0).Enabled);
        Assert.False(target.Get(1).Enabled);
    }

    [Fact]
    public void NullPageTextIsRejectedWithoutChangingWorkspace()
    {
        var target = new PageWorkspace();
        var original = target.Add();
        var bad = """[{"Id":"valid","Name":null,"Line1Template":"A","Line2Template":"B","DurationSeconds":5,"RefreshIntervalMilliseconds":1000,"ScrollStepMilliseconds":250,"ScrollEdgePauseMilliseconds":800}]""";
        Assert.Throws<InvalidOperationException>(() => target.ImportJson(bad));
        Assert.Single(target.Pages);
        Assert.Equal(original.Id, target.Get(0).Id);
    }


    [Fact]
    public void DuplicateIdentifiersAreRejectedTransactionally()
    {
        var workspace = new PageWorkspace();
        var original = workspace.Add();
        var json = workspace.ExportJson();
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var item = document.RootElement[0].GetRawText();
        Assert.Throws<InvalidOperationException>(() => workspace.ImportJson("[" + item + "," + item + "]"));
        Assert.Single(workspace.Pages);
        Assert.Equal(original.Id, workspace.Get(0).Id);
    }

    [Fact]
    public void NullPagesAreRejectedWithoutClearingWorkspace()
    {
        var workspace = new PageWorkspace();
        workspace.Add();
        Assert.Throws<InvalidOperationException>(() => workspace.ImportJson("[null]"));
        Assert.Single(workspace.Pages);
    }

    [Fact]
    public void DistinctIdentifiersRoundtrip()
    {
        var workspace = new PageWorkspace();
        workspace.Add();
        workspace.Add();
        var target = new PageWorkspace();
        target.ImportJson(workspace.ExportJson());
        Assert.Equal(2, target.Pages.Count);
    }


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
            workspace.ImportJson("""[{"Id":"valid","DurationSeconds":5,"RefreshIntervalMilliseconds":1000,"VisibilityExpression":"invalid"}]"""));
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

    [Theory]
    [InlineData("Unknown", "PingPong")]
    [InlineData("PingPong", "Invalid")]
    [InlineData(null, "Truncate")]
    [InlineData("Marquee", null)]
    public void InvalidOverflowImportPreservesExistingWorkspace(string? first, string? second)
    {
        var source = new PageWorkspace();
        var page = source.Add();
        page.Line1OverflowMode = first!;
        page.Line2OverflowMode = second!;
        var target = new PageWorkspace();
        var original = target.Add();
        original.Name = "Keep me";
        Assert.Throws<InvalidOperationException>(() => target.ImportJson(source.ExportJson()));
        Assert.Single(target.Pages);
        Assert.Equal("Keep me", target.Get(0).Name);
        Assert.Equal(original.Id, target.Get(0).Id);
    }

    [Theory]
    [InlineData("PingPong")]
    [InlineData("Marquee")]
    [InlineData("Truncate")]
    public void SupportedOverflowModesSurviveImport(string mode)
    {
        var source = new PageWorkspace();
        source.Add().Line1OverflowMode = mode;
        var target = new PageWorkspace();
        target.ImportJson(source.ExportJson());
        Assert.Equal(mode, target.Get(0).Line1OverflowMode);
    }
}
