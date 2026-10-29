using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using LIS2.App.Avalonia;
using LIS2.Core;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LIS2.App.Avalonia.Tests;

public sealed class UpdateUiTests
{
    [AvaloniaTheory]
    [InlineData("German", "de", 1305, 893)]
    [InlineData("Russian", "ru", 950, 600)]
    public async Task ManualCheckDownloadLocalizationAndPreferences(string language, string code, int width, int height)
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-update-ui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        await store.SaveAsync(new() { LanguageMode = language, CheckUpdatesAutomatically = true });
        var handler = new ReleaseHandler();
        using var client = new HttpClient(handler);
        var window = new MainWindow(store, new PortableHaTokenStore(directory, (_, _) => null), false,
            _ => new SerialDisplaySession(new VirtualLis2Transport()), updateClient: client) { Width = width, Height = height };
        try
        {
            window.Show();
            window.FindControl<Button>("NavButton7")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(100);
            Assert.Equal(0, handler.Calls); // Isolierte UI darf trotz gespeicherter Auswahl nicht ins Netz gehen.
            Assert.Equal(UpdateText.Get("Updates", code), window.FindControl<TextBlock>("UpdateHeading")!.Text);
            Click(window, "UpdateCheck");
            await WaitAsync(() => window.FindControl<Button>("UpdateCheck")!.IsEnabled);
            Assert.Contains("9.9.9", window.FindControl<TextBlock>("UpdateVersions")!.Text);
            Assert.True(window.FindControl<Button>("UpdateNotice")!.IsVisible);
            Assert.Equal(UpdateText.Get("Update available.", code), window.FindControl<TextBlock>("UpdateStatus")!.Text);
            window.FindControl<Expander>("UpdateNotesExpander")!.IsExpanded = true;
            await CaptureAsync(window, "updates-available-" + code);
            Click(window, "UpdateDownload");
            await WaitAsync(() => window.FindControl<Button>("UpdateCheck")!.IsEnabled);
            Assert.Equal(UpdateText.Get("Download verified.", code), window.FindControl<TextBlock>("UpdateStatus")!.Text);
            Assert.True(window.FindControl<Button>("UpdateFolder")!.IsVisible);
            Assert.True(window.FindControl<Button>("UpdateInstall")!.IsVisible);
            window.FindControl<CheckBox>("UpdateAutomaticCheck")!.IsChecked = false;
            await WaitAsync(() => !store.Load().CheckUpdatesAutomatically);
            Assert.False(store.Load().InstallUpdatesAutomatically);
            Assert.False(window.FindControl<CheckBox>("UpdateAutomaticInstall")!.IsEnabled);
            Assert.Equal(3, handler.Calls);
            await CaptureAsync(window, "updates-downloaded-" + code);
        }
        finally
        {
            await window.BeginShutdownAsync();
            window.Close();
            Directory.Delete(directory, true);
        }
    }

    [AvaloniaFact]
    public async Task NewWorkspaceDoesNotAuthorizeAutomaticChecksOrInstallation()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-update-pref-" + Guid.NewGuid().ToString("N"));
        var store = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        var saved = store.Load();
        Assert.False(saved.CheckUpdatesAutomatically);
        Assert.False(saved.InstallUpdatesAutomatically);
        saved.CheckUpdatesAutomatically = true;
        saved.InstallUpdatesAutomatically = true;
        saved.DebianUpdates = false;
        try
        {
            await store.SaveAsync(saved);
            var loaded = store.Load();
            Assert.True(loaded.CheckUpdatesAutomatically);
            Assert.True(loaded.InstallUpdatesAutomatically);
            Assert.False(loaded.DebianUpdates);
        }
        finally { Directory.Delete(directory, true); }
    }

    private static void Click(MainWindow window, string name) => window.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static async Task WaitAsync(Func<bool> ready)
    {
        var until = DateTime.UtcNow.AddSeconds(15);
        while (!ready())
        {
            Assert.True(DateTime.UtcNow < until, "Update UI timeout");
            await Task.Delay(20);
        }
    }
    private static async Task CaptureAsync(MainWindow window, string name)
    {
        await Task.Delay(60);
        window.UpdateLayout();
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        var output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../TestResults/screenshots"));
        Directory.CreateDirectory(output);
        frame.Save(Path.Combine(output, name + ".png"));
    }
    private sealed class ReleaseHandler : HttpMessageHandler
    {
        internal int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var name = ApplicationUpdates.AssetName(new Version(9, 9, 9), OperatingSystem.IsWindows(), RuntimeInformation.OSArchitecture, UpdateInstallation.DebianAvailable);
            var bytes = Encoding.UTF8.GetBytes("UI package fixture");
            string Url(string asset) => ApplicationUpdates.Repository + "/releases/download/v9.9.9/" + asset;
            HttpContent content = request.RequestUri!.AbsoluteUri == ApplicationUpdates.Endpoint
                ? new StringContent(JsonSerializer.Serialize(new { draft = false, prerelease = false, tag_name = "v9.9.9", body = "Test release notes\n\nA longer example explaining the changes, package installation and available display features.", assets = new[]
                {
                    new { name, browser_download_url = Url(name) },
                    new { name = "SHA256SUMS.txt", browser_download_url = Url("SHA256SUMS.txt") }
                }}))
                : request.RequestUri.AbsolutePath.EndsWith("SHA256SUMS.txt", StringComparison.Ordinal)
                    ? new StringContent(Convert.ToHexString(SHA256.HashData(bytes)) + "  " + name + "\n") : new ByteArrayContent(bytes);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }
}
