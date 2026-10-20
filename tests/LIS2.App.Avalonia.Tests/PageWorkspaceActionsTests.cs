using LIS2.App.Avalonia;
using LIS2.Display;

namespace LIS2.App.Avalonia.Tests;

public sealed class PageWorkspaceActionsTests
{
    [Fact]
    public async Task InvalidDraftKeepsSelectionAndWorkspaceAndSkipsPersistence()
    {
        var workspace = new PageWorkspace();
        workspace.Add().Name = "Existing";
        var selected = 0;
        var reloaded = false;
        var saved = false;
        var opened = false;
        var configured = false;
        var before = workspace.ExportJson();
        var actions = new PageWorkspaceActions(workspace, () => false,
            index => selected = index,
            () => { reloaded = true; return Task.CompletedTask; },
            () => { saved = true; return Task.CompletedTask; },
            () => opened = true);

        Assert.False(await actions.AddSourcePageAsync(page => configured = true));

        Assert.Equal(before, workspace.ExportJson());
        Assert.Equal(0, selected);
        Assert.False(configured);
        Assert.False(reloaded);
        Assert.False(saved);
        Assert.False(opened);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task ValidDraftAndSourcePageAreSavedAndReachEveryRunningSequence(bool virtualRunning, bool serialRunning)
    {
        var path = Path.Combine(Path.GetTempPath(), "lis2-source-page-" + Guid.NewGuid().ToString("N") + ".json");
        var workspace = new PageWorkspace();
        var original = workspace.Add();
        original.Line1Template = "Before edit";
        original.DurationSeconds = 1;
        var virtualFrames = new List<DisplayFrame>();
        var serialFrames = new List<DisplayFrame>();
        var virtualSequence = Sequence(virtualFrames);
        var serialSequence = Sequence(serialFrames);
        if (virtualRunning) Assert.True(virtualSequence.Start(workspace.Pages));
        if (serialRunning) Assert.True(serialSequence.Start(workspace.Pages));
        var values = new Dictionary<string, object?> { ["HA.sensor.cpu"] = "42" };
        var now = DateTimeOffset.UnixEpoch;
        await virtualSequence.TickAsync(now, values);
        await serialSequence.TickAsync(now, values);
        var store = new PortableWorkspaceStore(path);
        var selected = 0;
        var opened = false;
        var actions = new PageWorkspaceActions(workspace,
            () => { original.Name = "Edited name"; original.Line1Template = "Edited draft"; return true; },
            index => selected = index,
            async () =>
            {
                await virtualSequence.ReloadAsync(workspace.Pages);
                await serialSequence.ReloadAsync(workspace.Pages);
            },
            () => store.SaveAsync(new PortableWorkspace { Pages = workspace.Pages.ToList() }),
            () =>
            {
                Assert.True(store.Exists);
                opened = true;
            });
        try
        {
            Assert.True(await actions.AddSourcePageAsync(page =>
            {
                page.Name = "CPU";
                page.Line1Template = "{HA.sensor.cpu}";
                page.DurationSeconds = 1;
            }));

            Assert.Equal(1, selected);
            Assert.True(opened);
            var restored = store.Load();
            Assert.Equal(2, restored.Pages.Count);
            Assert.Equal("Edited name", restored.Pages[0].Name);
            Assert.Equal("Edited draft", restored.Pages[0].Line1Template);
            Assert.Equal("{HA.sensor.cpu}", restored.Pages[1].Line1Template);
            foreach (var (sequence, frames, running) in new[]
            {
                (virtualSequence, virtualFrames, virtualRunning),
                (serialSequence, serialFrames, serialRunning)
            })
            {
                Assert.Equal(running, sequence.IsRunning);
                await sequence.TickAsync(now.AddMilliseconds(1), values);
                await sequence.TickAsync(now.AddSeconds(2), values);
                if (running)
                {
                    Assert.Equal(3, frames.Count);
                    Assert.Equal("Edited draft".PadRight(20), frames[^2].Line1);
                    Assert.Equal("42".PadRight(20), frames[^1].Line1);
                    Assert.Equal(workspace.Get(1).Id, sequence.CurrentPageId);
                }
                else Assert.Empty(frames);
            }
        }
        finally
        {
            await virtualSequence.StopAndDrainAsync();
            await serialSequence.StopAndDrainAsync();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task ExplicitEmptyWorkspaceReplacesStartupPageAfterSaveAndLoad()
    {
        var path = Path.Combine(Path.GetTempPath(), "lis2-empty-pages-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var store = new PortableWorkspaceStore(path);
            Assert.False(store.Exists);
            var workspace = new PageWorkspace();
            workspace.Add();
            await store.SaveAsync(new PortableWorkspace { Pages = new() });
            Assert.True(store.Exists);

            PageWorkspaceActions.RestorePages(workspace, store.Load());

            Assert.Empty(workspace.Pages);
            var restarted = new PageWorkspace();
            restarted.Add();
            PageWorkspaceActions.RestorePages(restarted, store.Load());
            Assert.Empty(restarted.Pages);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void InvalidSavedPagesPreserveWorkspace()
    {
        var workspace = new PageWorkspace();
        var original = workspace.Add();
        var saved = new PortableWorkspace
        {
            Pages = new() { new() { DurationSeconds = 0 } }
        };

        Assert.Throws<InvalidOperationException>(() => PageWorkspaceActions.RestorePages(workspace, saved));

        Assert.Same(original, Assert.Single(workspace.Pages));
    }

    private static PageSequenceController Sequence(List<DisplayFrame> frames) => new(
        (frame, _) => { frames.Add(frame); return Task.CompletedTask; },
        _ => Task.CompletedTask);
}
