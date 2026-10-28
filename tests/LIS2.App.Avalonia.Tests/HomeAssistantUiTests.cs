using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class HomeAssistantUiTests
{
    [AvaloniaTheory]
    [InlineData("German", "de")]
    [InlineData("French", "fr")]
    [InlineData("Russian", "ru")]
    [InlineData("English", "en")]
    public async Task BrowserCreatesWorkingAliasAndAttributePagesAndPersistsSettings(string language, string code)
    {
        await using var server = new HomeAssistantServer();
        var directory = Path.Combine(Path.GetTempPath(), "lis2-ha-ui-" + Guid.NewGuid().ToString("N"));
        var workspace = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        await workspace.SaveAsync(new PortableWorkspace { LanguageMode = language });
        string? token = null;
        var tokenStore = new PortableHaTokenStore(directory, (operation, value) =>
        {
            if (operation == "store") token = value;
            if (operation == "clear") token = null;
            return operation == "lookup" ? token : null;
        });
        var sources = new PageSourceValues();
        await sources.StartAsync();
        var window = new MainWindow(workspace, tokenStore, false, _ => throw new InvalidOperationException(), sources);
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            window.Show();
            Click(window, "NavButton9");
            var local = window.FindControl<CheckBox>("HaLocalTokenStorage")!;
            Assert.Equal(OperatingSystem.IsLinux(), local.IsVisible);
            Assert.False(local.IsChecked);
            // Dieselben Linux-Controls werden auf Windows zur Sichtprüfung eingeblendet.
            local.IsVisible = true;
            Assert.Equal(UiTranslations.Translate("Store token in a local AES file", code), local.Content);
            window.FindControl<CheckBox>("HaEnabled")!.IsChecked = true;
            local.IsChecked = OperatingSystem.IsLinux();
            window.FindControl<TextBox>("HaUrl")!.Text = server.Url;
            window.FindControl<TextBox>("HaToken")!.Text = "fixture-token";
            Click(window, "HaConnect");
            var socket = await server.AcceptAsync();
            await server.InitializeAsync(socket);
            await server.SendAsync(socket, new { id = 2, type = "event", @event = new { event_type = "state_changed", data = new
            {
                entity_id = "sensor.room", new_state = new { entity_id = "sensor.room", state = "21.50",
                    attributes = new { friendly_name = "Room", unit_of_measurement = "°C", battery = 87, target = 21.5, version = "1.25" } }
            } } });
            await HomeAssistantServer.WaitAsync(() => sources.HomeAssistant.Entities.SingleOrDefault()?.State == "21.50");
            // Der Suchfilter stößt dieselbe Aktualisierung wie der periodische UI-Lauf an.
            window.FindControl<TextBox>("HaEntityFilter")!.Text = "room";
            await HomeAssistantServer.WaitAsync(() => Equals("sensor.room", window.FindControl<ComboBox>("HaEntityPicker")!.SelectedItem));
            Assert.Contains("21" + originalCulture.NumberFormat.NumberDecimalSeparator + "50", window.FindControl<TextBlock>("HaSelectedEntity")!.Text!);
            var attributeRows = window.FindControl<ListBox>("HaAttributes")!.Items.Cast<object>().Select(row => row.ToString()!).ToArray();
            Assert.Contains(attributeRows, row => row.Contains("Name = target") && row.Contains(21.5.ToString(originalCulture)));
            Assert.Contains(attributeRows, row => row.Contains("Name = version") && row.Contains("1.25"));
            window.FindControl<TextBox>("HaAlias")!.Text = "Room Temp";
            window.FindControl<CheckBox>("HaFavorite")!.IsChecked = true;
            Click(window, "HaSaveEntity");
            await HomeAssistantServer.WaitAsync(() => workspace.Load().HomeAssistantEntityPreferences.Any(preference => preference.Alias == "Room Temp"));
            window.FindControl<ListBox>("HaAttributes")!.SelectedIndex = 0;
            var attributes = window.FindControl<ListBox>("HaAttributes")!;
            attributes.BringIntoView();
            Assert.True(attributes.Focus());
            window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
            window.KeyReleaseQwerty(PhysicalKey.C, RawInputModifiers.Control);
            await Task.Delay(30);
            var clipboard = Assert.IsAssignableFrom<IClipboard>(window.Clipboard);
            Assert.Equal("{HA.Room_Temp.attribute.battery}", await clipboard.TryGetTextAsync());
            await clipboard.SetTextAsync("");
            await DoubleClickFirstRow(window, attributes);
            Assert.Equal("{HA.Room_Temp.attribute.battery}", await clipboard.TryGetTextAsync());
            Click(window, "HaCreateAttributePage");
            await HomeAssistantServer.WaitAsync(() => workspace.Load().Pages.Any(page => page.Line2Template == "{HA.Room_Temp.attribute.battery|fallback:--}"));
            Assert.Equal(87L, sources.Snapshot(DateTimeOffset.Now)["HA.Room_Temp.attribute.battery"]);
            Click(window, "NavButton9");
            window.FindControl<ScrollViewer>("HomeAssistantView")!.Offset = default;
            await CaptureAsync(window, "port05-ha-" + code + "-wide");
            window.Width = 950;
            window.Height = 600;
            await CaptureAsync(window, "port05-ha-" + code + "-small");
            window.FindControl<ScrollViewer>("HomeAssistantView")!.ScrollToEnd();
            await CaptureAsync(window, "port05-ha-" + code + "-attributes");
            Assert.True(workspace.Load().HomeAssistantEnabled);
            Assert.Equal(OperatingSystem.IsLinux(), workspace.Load().HomeAssistantLocalTokenStorage);
            if (OperatingSystem.IsLinux())
            {
                var restored = new PortableHaTokenStore(directory) { UseLocalStorage = workspace.Load().HomeAssistantLocalTokenStorage };
                Assert.Equal("fixture-token", restored.Load());
            }
            Assert.True(workspace.Load().HomeAssistantEntityPreferences.Single().IsFavorite);
            Assert.DoesNotContain("fixture-token", await File.ReadAllTextAsync(workspace.FilePath));
            await server.SendAsync(socket, new { id = 2, type = "result", success = false });
            await HomeAssistantServer.WaitAsync(() => !sources.HomeAssistant.IsConnected);
            window.FindControl<TextBox>("HaEntityFilter")!.Text = "disconnected";
            await HomeAssistantServer.WaitAsync(() => window.FindControl<ListBox>("HaEntityList")!.ItemCount == 0);
            Assert.Contains(UiTranslations.Translate("Home Assistant state or subscription request failed.", code), window.FindControl<TextBlock>("HaStatus")!.Text);
            window.FindControl<CheckBox>("HaEnabled")!.IsChecked = false;
            Click(window, "HaConnect");
            await HomeAssistantServer.WaitAsync(() => window.FindControl<Button>("HaConnect")!.IsEnabled);
            Assert.False(workspace.Load().HomeAssistantEnabled);
            Assert.False(File.Exists(Path.Combine(directory, "ha-token.bin")));
            Assert.Null(token);
            Assert.Empty(sources.HomeAssistant.Entities);
            Assert.Equal("", window.FindControl<TextBox>("HaToken")!.Text);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            await window.BeginShutdownAsync();
            window.Close();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [AvaloniaFact]
    public async Task StorageFailureRemainsVisibleAndDoesNotStartConnection()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lis2-ha-ui-" + Guid.NewGuid().ToString("N"));
        var workspace = new PortableWorkspaceStore(Path.Combine(directory, "workspace.json"));
        await workspace.SaveAsync(new PortableWorkspace { LanguageMode = "German", HomeAssistantEnabled = false });
        var sources = new PageSourceValues();
        var tokenStore = new PortableHaTokenStore(directory, (_, _) => throw new IOException("Secret Service request timed out."));
        var window = new MainWindow(workspace, tokenStore, false, _ => throw new InvalidOperationException(), sources);
        try
        {
            window.Show();
            Click(window, "NavButton9");
            window.FindControl<CheckBox>("HaEnabled")!.IsChecked = true;
            window.FindControl<TextBox>("HaUrl")!.Text = "http://127.0.0.1:1";
            window.FindControl<TextBox>("HaToken")!.Text = "fixture-token";
            Click(window, "HaConnect");
            await HomeAssistantServer.WaitAsync(() => window.FindControl<Button>("HaConnect")!.IsEnabled);
            var message = window.FindControl<TextBlock>("HaActionStatus")!;
            Assert.True(message.IsVisible);
            Assert.Contains("Zeitüberschreitung", message.Text);
            window.FindControl<TextBox>("HaEntityFilter")!.Text = "refresh";
            Assert.Contains("Zeitüberschreitung", message.Text);
            Assert.False(sources.HomeAssistant.IsConfigured);
            Assert.False(workspace.Load().HomeAssistantEnabled);
            await CaptureAsync(window, "port05-ha-de-storage-error");
            await workspace.SaveAsync(new PortableWorkspace { HomeAssistantEnabled = true });
            window.FindControl<CheckBox>("HaEnabled")!.IsChecked = false;
            Click(window, "HaConnect");
            await HomeAssistantServer.WaitAsync(() => window.FindControl<Button>("HaConnect")!.IsEnabled);
            Assert.False(workspace.Load().HomeAssistantEnabled);
            Assert.Contains("Zeitüberschreitung", message.Text);
        }
        finally
        {
            await window.BeginShutdownAsync();
            window.Close();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static void Click(Window window, string name) => window.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task DoubleClickFirstRow(Window window, ListBox list)
    {
        list.BringIntoView();
        await Task.Delay(60);
        window.UpdateLayout();
        var row = list.ContainerFromIndex(0)!;
        var point = row.TranslatePoint(new Point(15, 15), window)!.Value;
        for (var count = 0; count < 2; count++)
        {
            window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        }
        await Task.Delay(30);
    }

    private static async Task CaptureAsync(Window window, string name)
    {
        await Task.Delay(60);
        window.UpdateLayout();
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../TestResults/screenshots"));
        Directory.CreateDirectory(directory);
        frame.Save(Path.Combine(directory, name + ".png"));
    }
}
