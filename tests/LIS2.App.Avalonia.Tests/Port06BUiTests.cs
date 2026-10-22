using System.Reflection;
using System.Text;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LIS2.App.Avalonia;
using LIS2.Core;
using LIS2.Display;
using LIS2.Runtime;

namespace LIS2.App.Avalonia.Tests;

public sealed class Port06BUiTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SavingFanChannelsUpdatesRunningOutputAndKeepsAuthorizationExplicit(bool serial)
    {
        await using var fixture = await Fixture.OpenAsync(serial);
        var window = fixture.Window;
        fixture.Click("NavButton6");
        window.FindControl<CheckBox>("FanHardwareConsent")!.IsChecked = true;
        fixture.Click(serial ? "FanHardwareStart" : "FanVirtualStart");
        await Wait(() => fixture.FanState(serial)?.Fan1 == 40);
        fixture.Text("FanFixed", "80");
        fixture.Click("FanSave");
        await Wait(() => fixture.Store.Load().Fans.Channels[0].FixedPercent == 80 && fixture.FanState(serial)?.Fan1 == 80);

        fixture.Text("FanMinimum", "60");
        fixture.Text("FanMaximum", "70");
        fixture.Text("FanFailSafe", "65");
        fixture.Click("FanSave");
        await Wait(() => fixture.FanState(serial)?.Fan1 == 70);
        window.FindControl<ComboBox>("FanModePicker")!.SelectedIndex = 1;
        fixture.Text("FanSensorKey", "Hardware.Missing.Temperature");
        fixture.Click("FanSave");
        await Wait(() => fixture.FanState(serial)?.Fan1 == 65);

        fixture.Click(serial ? "FanHardwareStop" : "FanVirtualStop");
        await Wait(() => serial ? !fixture.Serial!.FansRunning : Field<AuthorizedFanRuntimeSession?>(window, "_virtualFanSession") is null);
        var transport = serial ? fixture.Transport : fixture.LastVirtualFanTransport!;
        var writes = transport.Writes.Count(IsFanCommand);
        fixture.Text("FanFixed", "90");
        fixture.Click("FanSave");
        await Wait(() => fixture.Store.Load().Fans.Channels[0].FixedPercent == 90);
        Assert.Equal(writes, transport.Writes.Count(IsFanCommand));
        Assert.False(serial ? fixture.Serial!.FansRunning : Field<AuthorizedFanRuntimeSession?>(window, "_virtualFanSession") is not null);
    }

    [AvaloniaFact]
    public async Task SavingFanChannelsAlsoUpdatesContinuousDryRun()
    {
        await using var fixture = await Fixture.OpenAsync();
        fixture.Click("NavButton6");
        fixture.Click("FanMonitorStart");
        await Wait(() => fixture.Window.FindControl<TextBlock>("FanPreviewStatus")!.Text?.Contains("40%") == true);
        fixture.Text("FanFixed", "80");
        fixture.Click("FanSave");
        await Wait(() => fixture.Window.FindControl<TextBlock>("FanPreviewStatus")!.Text?.Contains("80%") == true);
        Assert.Empty(fixture.Transport.Writes);
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DeviceSettingsPreserveRotationOverlayAndStoppedState(bool serial, bool glyphs)
    {
        await using var fixture = await Fixture.OpenAsync(serial);
        var window = fixture.Window;
        await InvokeAsync(window, serial ? "StartSerialPlaybackAsync" : "StartPagePlaybackAsync");
        var sequence = Field<PageSequenceController>(window, serial ? "_serialSequence" : "_virtualSequence");
        var page = sequence.CurrentPageId;
        sequence.Events.Add(new DisplayEvent("review", DisplayFrame.Create("OVERLAY", "Keep running"), 100, DateTimeOffset.UtcNow.AddSeconds(30)));
        Assert.True(sequence.IsRunning);
        if (glyphs) await InvokeAsync(window, "SaveAndSynchronizeGlyphSetAsync");
        else await InvokeAsync(window, "SetSelectedBrightnessAsync", 50);
        Assert.True(sequence.IsRunning);
        Assert.Equal(page, sequence.CurrentPageId);
        Assert.Contains(sequence.Events.Snapshot(DateTimeOffset.UtcNow), item => item.Id == "review");
        var timer = Field<AdaptiveRuntimeLoop>(window, serial ? "_serialPlaybackTimer" : "_pageTimer");
        Assert.True(timer.IsRunning);
        await Wait(() => (serial ? fixture.Transport.State.Line1 : Field<VirtualDeviceSession>(window, "_virtualDevice").State.Line1).StartsWith("OVERLAY"));

        Invoke(window, serial ? "StopSerialPlayback" : "StopPagePlayback");
        if (glyphs) await InvokeAsync(window, "SaveAndSynchronizeGlyphSetAsync");
        else await InvokeAsync(window, "SetSelectedBrightnessAsync", 75);
        Assert.False(sequence.IsRunning);
        Assert.False(timer.IsRunning);
    }

    [AvaloniaFact]
    public async Task ConditionStatusTracksSourceChangesAndLanguageChanges()
    {
        await using var sources = new PageSourceValues();
        sources.UpdateHardware(new Dictionary<string, object?> { ["Hardware.CPU.Load.Total"] = 25d });
        await using var fixture = await Fixture.OpenAsync(sources: sources);
        var window = fixture.Window;
        fixture.Click("NavButton2");
        fixture.Text("PageVisibility", "Hardware.CPU.Load.Total>50");
        var status = window.FindControl<TextBlock>("PageConditionStatus")!;
        await Wait(() => status.Text == "Condition currently does not match.");
        sources.UpdateHardware(new Dictionary<string, object?> { ["Hardware.CPU.Load.Total"] = 75d });
        Invoke(window, "TickEditorPreview");
        Assert.Equal("Condition currently matches.", status.Text);
        window.FindControl<ComboBox>("LanguageModeComboBox")!.SelectedIndex = 2;
        await Wait(() => status.Text != "Condition currently matches.");
        Assert.False(string.IsNullOrWhiteSpace(status.Text));
        await fixture.CaptureAsync("port06b-condition");
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PageImportReportsPersistenceResultAndPreservesFileOnFailure(bool failSave)
    {
        await using var fixture = await Fixture.OpenAsync();
        var json = JsonSerializer.Serialize(new[] { new PageDefinition { Name = "Imported", Line1Template = "IMPORTED" } });
        fixture.UseImportFile(json);
        if (failSave) Directory.CreateDirectory(fixture.Store.FilePath + ".tmp");
        fixture.Click("NavButton2");
        fixture.Click("PageImport");
        var status = fixture.Window.FindControl<TextBlock>("VirtualStatus")!;
        await Wait(() => status.Text?.StartsWith(failSave ? "Page file error:" : "Page definitions imported.") == true);
        Assert.Equal(failSave ? "Review" : "Imported", fixture.Store.Load().Pages.Single().Name);
        if (failSave)
        {
            Assert.DoesNotContain("Page definitions imported.", status.Text);
            await fixture.CaptureAsync("port06b-import-error");
            Directory.Delete(fixture.Store.FilePath + ".tmp");
        }
    }

    private static T Field<T>(MainWindow window, string name) =>
        (T)typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;

    private static bool IsFanCommand(byte[] command) => command.Length >= 2 && command[0] == 0xAE && command[1] == 0xF0;

    private static object? Invoke(MainWindow window, string name, params object[] args) =>
        typeof(MainWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, args);

    private static Task InvokeAsync(MainWindow window, string name, params object[] args) => (Task)Invoke(window, name, args)!;

    private static async Task Wait(Func<bool> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(6);
        while (!predicate() && DateTime.UtcNow < deadline) await Task.Delay(20);
        Assert.True(predicate());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "lis2-port06b-" + Guid.NewGuid().ToString("N"));
        public MainWindow Window { get; private set; } = null!;
        public PortableWorkspaceStore Store { get; private set; } = null!;
        public VirtualLis2Transport Transport { get; } = new();
        public SerialDisplaySession? Serial { get; private set; }
        public VirtualLis2Transport? LastVirtualFanTransport { get; private set; }

        public static async Task<Fixture> OpenAsync(bool serial = false, PageSourceValues? sources = null)
        {
            var fixture = new Fixture();
            fixture.Store = new PortableWorkspaceStore(Path.Combine(fixture._directory, "workspace.json"));
            var workspace = new PortableWorkspace { LanguageMode = "English", ProgramCustomGlyphsOnConnect = true,
                TransportMode = serial ? "Serial" : "Virtual", PortName = "isolated-test" };
            workspace.Pages.Add(new PageDefinition { Name = "Review", Line1Template = "Review", Line2Template = "{Clock.Time}" });
            workspace.Fans.Channels[0].FixedPercent = 40;
            await fixture.Store.SaveAsync(workspace);
            fixture.Window = new MainWindow(fixture.Store, new PortableHaTokenStore(fixture._directory, "no-keyring"), false,
                _ => fixture.Serial = new SerialDisplaySession(fixture.Transport), sources, new SimulatedFanSensors());
            fixture.Window.Show();
            await Wait(() => fixture.Window.FindControl<TextBlock>("ConnectionText")!.Text?.StartsWith("Connected") == true &&
                fixture.Window.FindControl<Button>("SettingsApplyTransport")!.IsEnabled);
            await fixture.Store.SaveAsync(fixture.Store.Load());
            return fixture;
        }

        public void Click(string name) => Window.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        public void Text(string name, string value) => Window.FindControl<TextBox>(name)!.Text = value;

        public VirtualLis2State? FanState(bool serial)
        {
            if (serial) return Transport.State;
            LastVirtualFanTransport = Field<VirtualLis2Transport?>(Window, "_virtualFanTransport");
            return LastVirtualFanTransport?.State;
        }

        public void UseImportFile(string json)
        {
            var file = DispatchProxy.Create<IStorageFile, ImportStorageProxy>();
            ((ImportStorageProxy)(object)file).Handler = method => method.Name == "OpenReadAsync"
                ? Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(json)))
                : throw new InvalidOperationException(method.Name);
            var provider = DispatchProxy.Create<IStorageProvider, ImportStorageProxy>();
            ((ImportStorageProxy)(object)provider).Handler = method => method.Name == "OpenFilePickerAsync"
                ? Task.FromResult<IReadOnlyList<IStorageFile>>(new[] { file })
                : throw new InvalidOperationException(method.Name);
            typeof(TopLevel).GetField("_storageProvider", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(Window, provider);
        }

        public async Task CaptureAsync(string name)
        {
            Window.Width = 950;
            Window.Height = 600;
            await Task.Delay(40);
            Window.UpdateLayout();
            if (name == "port06b-condition")
            {
                Window.FindControl<TextBlock>("PageConditionStatus")!.BringIntoView();
                await Task.Delay(40);
                Window.UpdateLayout();
            }
            using var frame = Window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../..", "TestResults", "screenshots"));
            Directory.CreateDirectory(directory);
            frame.Save(Path.Combine(directory, name + ".png"));
        }

        public async ValueTask DisposeAsync()
        {
            await Window.BeginShutdownAsync();
            Window.Close();
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }
    }

    public class ImportStorageProxy : DispatchProxy
    {
        public Func<MethodInfo, object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!);
    }
}
