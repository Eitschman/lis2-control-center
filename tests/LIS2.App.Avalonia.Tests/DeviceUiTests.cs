using Avalonia.Controls;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using LIS2.App.Avalonia;
using LIS2.Core;

namespace LIS2.App.Avalonia.Tests;

public sealed class DeviceUiTests
{
    [AvaloniaTheory]
    [InlineData(950, 600, 1, "en")]
    [InlineData(950, 600, 2, "de")]
    [InlineData(1305, 893, 2, "de")]
    [InlineData(950, 600, 3, "fr")]
    [InlineData(950, 600, 4, "tr")]
    [InlineData(950, 600, 5, "ru")]
    public async Task FormattingEditorBuildsPreviewsAndInsertsWithoutReplacingFreeEditing(int width, int height, int languageIndex, string language)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        // Dispatcherereignisse verwenden die Systemkultur, unabhängig von der gewählten App-Sprache.
        try
        {
            await using var ui = await Fixture.OpenAsync(new PortableWorkspace());
            ui.Window.Width = width;
            ui.Window.Height = height;
            ui.Control<ComboBox>("LanguageModeComboBox").SelectedIndex = languageIndex;
            ui.Click("NavButton2");
            ui.Click("PageAdd");
            ui.Control<TextBox>("PageLine1").Text = "CPU=";
            ui.Control<TextBox>("PageLine1").CaretIndex = 4;
            ui.Control<TextBox>("PageLine2").Text = "";
            ui.Control<Expander>("PageFormattingEditor").IsExpanded = true;
            ui.Control<TextBox>("FormatVariable").Text = "Clock.Year";
            ui.Control<ComboBox>("FormatKind").SelectedIndex = 1;
            ui.Control<NumericUpDown>("FormatDecimals").Value = 2;
            ui.Control<TextBox>("FormatSuffix").Text = "°C";
            await Fixture.WaitUntilAsync(() => ui.Control<TextBox>("FormatTemplate").Text == "{Clock.Year|number:2|suffix:°C}");
            Assert.Equal("{Clock.Year|number:2|suffix:°C}", ui.Control<TextBox>("FormatTemplate").Text);
            Assert.Equal(DateTime.Now.Year.ToString("F2", culture) + "°C", ui.Control<TextBlock>("FormatResult").Text);
            Assert.Equal(UiTranslations.Translate("Formatting editor", language), ui.Control<Expander>("PageFormattingEditor").Header);
            foreach (var name in new[] { "PageLine1", "PageLine2" })
            {
                var tip = Assert.IsType<string>(ToolTip.GetTip(ui.Control<TextBox>(name)));
                Assert.Contains("{Key|number:2}", tip);
                Assert.Contains("50,12", tip);
                if (language != "en") Assert.DoesNotContain("German regional settings", tip);
            }
            ui.Click("FormatInsert1");
            ui.Click("FormatInsert2");
            Assert.Equal("CPU={Clock.Year|number:2|suffix:°C}", ui.Control<TextBox>("PageLine1").Text);
            Assert.Equal("{Clock.Year|number:2|suffix:°C}", ui.Control<TextBox>("PageLine2").Text);
            ui.Control<TextBox>("FormatVariable").Text = "Missing.Value";
            ui.Control<TextBox>("FormatFallback").Text = "--";
            await Fixture.WaitUntilAsync(() => ui.Control<TextBlock>("FormatResult").Text == "--");
            Assert.Equal("--", ui.Control<TextBlock>("FormatResult").Text);
            Assert.NotEmpty(ui.Control<TextBlock>("FormatStatus").Text!);
            ui.Control<TextBox>("FormatSuffix").Text = "|upper";
            await Fixture.WaitUntilAsync(() => !ui.Control<Button>("FormatInsert1").IsEnabled);
            Assert.False(ui.Control<Button>("FormatInsert1").IsEnabled);
            ui.Control<TextBox>("FormatSuffix").Text = "°C";
            ui.Control<TextBox>("FormatVariable").Text = "Clock.Year";
            await Fixture.WaitUntilAsync(() => ui.Control<Button>("FormatInsert1").IsEnabled);
            ui.Control<NumericUpDown>("FormatDecimals").Value = null;
            Assert.False(ui.Control<Button>("FormatInsert1").IsEnabled);
            ui.Control<ComboBox>("FormatKind").SelectedIndex = 0;
            Assert.True(ui.Control<Button>("FormatInsert1").IsEnabled);
            ui.Control<ComboBox>("FormatKind").SelectedIndex = 1;
            // Die Bilder verwenden eine fachlich passende Uhrzeitformatierung statt Testeinheiten am Jahr.
            ui.Control<TextBox>("FormatVariable").Text = "Clock.Hour";
            ui.Control<NumericUpDown>("FormatDecimals").Value = 0;
            ui.Control<TextBox>("FormatSuffix").Text = "h";
            await Fixture.WaitUntilAsync(() => ui.Control<TextBox>("FormatTemplate").Text?.Contains("suffix:h}") == true);
            ui.Control<Button>("FormatInsert1").Focus();
            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { },
                global::Avalonia.Threading.DispatcherPriority.Background);
            ui.Window.UpdateLayout();
            var scroll = ui.Control<ScrollViewer>("PagesView");
            var editor = ui.Control<Expander>("PageFormattingEditor");
            var position = editor.TranslatePoint(default, (global::Avalonia.Visual)scroll.Content!)!.Value;
            scroll.Offset = new Vector(0, position.Y);
            await ui.CaptureAsync($"formatting-editor-top-{language}-{width}x{height}");
            ui.Control<Button>("FormatInsert2").BringIntoView();
            await ui.CaptureAsync($"formatting-editor-{language}-{width}x{height}");
            await ui.ClickAndWaitAsync("PageSave");
            await Fixture.WaitUntilAsync(() => ui.Store.Load().Pages.Any(page => page.Line1Template == "CPU={Clock.Year|number:2|suffix:°C}"));
            Assert.Contains(ui.Store.Load().Pages, page => page.Line1Template == "CPU={Clock.Year|number:2|suffix:°C}");
            ui.Control<TextBox>("FormatPrefix").Text = " Core 10: ";
            ui.Control<TextBox>("FormatSuffix").Text = " % ";
            await Fixture.WaitUntilAsync(() => ui.Control<TextBox>("FormatTemplate").Text ==
                "{Clock.Hour|fallback:--|number:0|prefix: Core 10: |suffix: % }");
            Assert.Equal(" Core 10: " + DateTime.Now.Hour.ToString(culture) + " % ",
                ui.Control<TextBlock>("FormatResult").Text);
            ui.Control<TextBox>("PageLine2").Text = "";
            ui.Click("FormatInsert2");
            const string spacedTemplate = "{Clock.Hour|fallback:--|number:0|prefix: Core 10: |suffix: % }";
            Assert.Equal(spacedTemplate, ui.Control<TextBox>("PageLine2").Text);
            await ui.ClickAndWaitAsync("PageSave");
            await Fixture.WaitUntilAsync(() => ui.Store.Load().Pages.Any(page => page.Line2Template == spacedTemplate));
            if (width == 1305)
            {
                ui.Control<Button>("FormatInsert2").BringIntoView();
                await ui.CaptureAsync("formatting-affix-spaces-de");
            }
            ui.Click("FormatBrowse");
            var browser = ui.Window.OwnedWindows.Single();
            browser.GetVisualDescendants().OfType<TextBox>().Single(control => control.Name == "VariableSearch").Text = "Clock.Year";
            await Fixture.WaitUntilAsync(() => browser.GetVisualDescendants().OfType<ListBox>().Single(control => control.Name == "VariableResults").ItemCount == 1);
            browser.GetVisualDescendants().OfType<Button>().Single(control => control.Name == "VariableInsert1")
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal("Clock.Year", ui.Control<TextBox>("FormatVariable").Text);
            ui.Window.RefreshMediaSource("Winamp", new Dictionary<string, object?>
            {
                ["Connected"] = true, ["Artist"] = "", ["StreamName"] = "RADIO BOB", ["Title"] = "Lacrimosa - Titel"
            });
            ui.Control<TextBox>("FormatVariable").Text = "Winamp.Artist";
            ui.Control<ComboBox>("FormatKind").SelectedIndex = 0;
            ui.Control<TextBox>("FormatPrefix").Text = "";
            ui.Control<TextBox>("FormatSuffix").Text = "";
            ui.Click("FormatFallbackBrowse");
            browser = ui.Window.OwnedWindows.Single();
            browser.GetVisualDescendants().OfType<TextBox>().Single(control => control.Name == "VariableSearch").Text = "Winamp.StreamName";
            await Fixture.WaitUntilAsync(() => browser.GetVisualDescendants().OfType<ListBox>().Single(control => control.Name == "VariableResults").ItemCount == 1);
            browser.GetVisualDescendants().OfType<Button>().Single(control => control.Name == "VariableInsert1")
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            const string fallbackTemplate = "{Winamp.Artist|fallbackvar:Winamp.StreamName|fallback:--}";
            await Fixture.WaitUntilAsync(() => ui.Control<TextBox>("FormatTemplate").Text == fallbackTemplate);
            Assert.Equal("RADIO BOB", ui.Control<TextBlock>("FormatResult").Text);
            Assert.Equal(UiTranslations.Translate("The fallback variable is used.", language), ui.Control<TextBlock>("FormatStatus").Text);
            ui.Control<TextBox>("PageLine1").Text = "";
            ui.Click("FormatInsert1");
            await ui.ClickAndWaitAsync("PageSave");
            await Fixture.WaitUntilAsync(() => ui.Store.Load().Pages.Any(page => page.Line1Template == fallbackTemplate));
            editor.BringIntoView();
            await ui.CaptureAsync($"fallback-editor-top-{language}-{width}x{height}");
            ui.Control<Button>("FormatInsert2").BringIntoView();
            await ui.CaptureAsync($"fallback-editor-{language}-{width}x{height}");
            ui.Control<TextBox>("FormatFallbackVariable").Text = "Bad|Key";
            await Fixture.WaitUntilAsync(() => !ui.Control<Button>("FormatInsert1").IsEnabled);
            ui.Control<TextBox>("FormatFallbackVariable").Text = "Winamp.StreamName";
            ui.Window.RefreshMediaSource("Winamp", new Dictionary<string, object?> { ["Connected"] = true, ["Artist"] = "Björk", ["StreamName"] = "RADIO BOB" });
            ui.Control<TextBox>("FormatVariable").Text = "Clock.Year";
            ui.Control<TextBox>("FormatVariable").Text = "Winamp.Artist";
            await Fixture.WaitUntilAsync(() => ui.Control<TextBlock>("FormatResult").Text == "Björk");
            var input = ui.Control<TextBox>("PageLine1");
            input.Focus();
            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { },
                global::Avalonia.Threading.DispatcherPriority.Background);
            ui.Window.KeyPressQwerty(PhysicalKey.F1, RawInputModifiers.None);
            Assert.True(ToolTip.GetIsOpen(input));
            Assert.Equal(PlacementMode.Bottom, ToolTip.GetPlacement(input));
            if (width == 1305)
            {
                await Task.Delay(200);
                await ui.CaptureAsync("formatting-tooltip-de");
            }
            ui.Window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            Assert.False(ToolTip.GetIsOpen(input));
            ui.Control<ListBox>("PagesList").SelectedIndex = -1;
            Assert.False(ui.Control<Button>("FormatInsert1").IsEnabled);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = culture; }
    }

    [AvaloniaTheory]
    [InlineData(950, 600, false)]
    [InlineData(1305, 893, false)]
    [InlineData(950, 600, true)]
    [InlineData(1305, 893, true)]
    public async Task PagePreviewMatchesDashboardForFullAndEmptyFrames(int width, int height, bool german)
    {
        await using var ui = await Fixture.OpenAsync(new PortableWorkspace());
        ui.Window.Width = width;
        ui.Window.Height = height;
        if (german) ui.Control<ComboBox>("LanguageModeComboBox").SelectedIndex = 2;
        ui.Click("NavButton1");
        ui.Control<TextBox>("Line1TextBox").Text = "12345678901234567890";
        ui.Control<TextBox>("Line2TextBox").Text = "ABCDEFGHIJKLMNOPQRST";
        await ui.ClickAndWaitAsync("RenderBoth");
        ui.Click("NavButton0");
        ui.Window.UpdateLayout();
var dashboardFirst = ui.Control<VfdLine>("PreviewLine1");
        var dashboardDisplay = Assert.IsType<Border>(Assert.IsType<StackPanel>(dashboardFirst.Parent).Parent);
        var expectedSize = dashboardDisplay.Bounds.Size;

        ui.Click("NavButton2");
        ui.Click("PageAdd");
        ui.Control<TextBox>("PageLine1").Text = "12345678901234567890";
        ui.Control<TextBox>("PageLine2").Text = "ABCDEFGHIJKLMNOPQRST";
        await Fixture.WaitUntilAsync(() => ui.Control<VfdLine>("PagePreview2").Text == "ABCDEFGHIJKLMNOPQRST");
        // PageAdd fokussiert den Namen; dessen verzögertes BringIntoView darf die Aufnahme nicht zurückscrollen.
        ui.Control<Button>("PageSave").Focus();
        await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { },
            global::Avalonia.Threading.DispatcherPriority.Background);
        ui.Window.UpdateLayout();
        var scroll = ui.Control<ScrollViewer>("PagesView");
        scroll.Offset = new Vector(0, double.MaxValue);
        await ui.CaptureAsync($"pages-vfd-{(german ? "de" : "en")}-{width}x{height}");
        var first = ui.Control<VfdLine>("PagePreview1");
        var second = ui.Control<VfdLine>("PagePreview2");
        var display = Assert.IsType<Border>(Assert.IsType<StackPanel>(first.Parent).Parent);
        Assert.InRange(scroll.Offset.Y, Math.Max(0, scroll.Extent.Height - scroll.Viewport.Height) - 1,
            Math.Max(0, scroll.Extent.Height - scroll.Viewport.Height) + 1);
        Assert.Equal(20, first.Text!.Length);
        Assert.Equal(20, second.Text!.Length);
        Assert.Equal(dashboardFirst.FontSize, first.FontSize);
        Assert.InRange(Math.Abs(display.Bounds.Width - expectedSize.Width), 0, 1);
        Assert.InRange(Math.Abs(display.Bounds.Height - expectedSize.Height), 0, 1);
        Assert.True(first.Bounds.Width >= first.DesiredSize.Width - 1);
        Assert.True(second.Bounds.Width >= second.DesiredSize.Width - 1);

        ui.Control<TextBox>("PageLine1").Text = "";
        ui.Control<TextBox>("PageLine2").Text = "";
        await Fixture.WaitUntilAsync(() => first.Text == new string(' ', 20) && second.Text == new string(' ', 20));
        ui.Window.UpdateLayout();
        Assert.InRange(Math.Abs(display.Bounds.Width - expectedSize.Width), 0, 1);
        Assert.InRange(Math.Abs(display.Bounds.Height - expectedSize.Height), 0, 1);
        ui.Control<ListBox>("PagesList").SelectedIndex = -1;
        await Fixture.WaitUntilAsync(() => first.Text == new string(' ', 20) && second.Text == new string(' ', 20));
        ui.Window.UpdateLayout();
        Assert.InRange(Math.Abs(display.Bounds.Width - expectedSize.Width), 0, 1);
        Assert.InRange(Math.Abs(display.Bounds.Height - expectedSize.Height), 0, 1);
    }

    [AvaloniaTheory]
    [InlineData(950, 600)]
    [InlineData(1305, 893)]
    public async Task SpectrumAndCustomGlyphFramesKeepTwentyFixedPreviewCells(int width, int height)
    {
        await using var ui = await Fixture.OpenAsync(new PortableWorkspace());
        ui.Window.Width = width;
        ui.Window.Height = height;
        Size? expected = null;
        foreach (var page in new[] { false, true })
        {
            ui.Click(page ? "NavButton2" : "NavButton0");
            var first = ui.Control<VfdLine>(page ? "PagePreview1" : "PreviewLine1");
            var second = ui.Control<VfdLine>(page ? "PagePreview2" : "PreviewLine2");
            var display = Assert.IsType<Border>(Assert.IsType<StackPanel>(first.Parent).Parent);
            var frames = new[] { new string(' ', 20), "12345678901234567890",
                "ABCDEFGHIJKLMNOPQRST", "ÄÖÜäöüß°µ→←↑↓ ±×÷≤≥·", "▁▂▃▄▅▆▇█▁▂▃▄▅▆▇█▁▂▃▄",
                "①②③④⑤⑥⑦⑧①②③④⑤⑥⑦⑧①②③④" }
                .Concat("▁▂▃▄▅▆▇█".Select(glyph => new string(glyph, 20)));
            foreach (var frame in frames)
            {
                first.Text = frame;
                second.Text = frame;
                ui.Window.UpdateLayout();
                expected ??= display.Bounds.Size;
                Assert.InRange(Math.Abs(display.Bounds.Width - expected.Value.Width), 0, 1);
                Assert.InRange(Math.Abs(display.Bounds.Height - expected.Value.Height), 0, 1);
                Assert.InRange(first.Bounds.Width, first.DesiredSize.Width - 1, first.DesiredSize.Width + 1);
                Assert.Equal(20, first.Text!.Length);
                var peer = global::Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(first);
                Assert.Equal(frame, peer.GetName());
                Assert.Equal(global::Avalonia.Automation.Peers.AutomationControlType.Text, peer.GetAutomationControlType());
            }
            first.Text = "▁▂▃▄▅▆▇█▁▂▃▄▅▆▇█▁▂▃▄";
            second.Text = "ÄÖÜäöüß°µ→←↑↓ ±×÷≤≥·";
            if (page)
            {
                ui.Control<ScrollViewer>("PagesView").Offset = new Vector(0, double.MaxValue);
                await ui.CaptureAsync($"pages-vfd-fixed-spectrum-{width}x{height}");
            }
            else await ui.CaptureAsync($"dashboard-vfd-fixed-spectrum-{width}x{height}");
        }
    }

    [AvaloniaTheory]
    [InlineData(950, 600, false)]
    [InlineData(1305, 893, false)]
    [InlineData(950, 600, true)]
    [InlineData(1305, 893, true)]
    public async Task DashboardPreviewFitsTwentyColumnsAndTwoRows(int width, int height, bool german)
    {
        await using var ui = await Fixture.OpenAsync(new PortableWorkspace());
        ui.Window.Width = width;
        ui.Window.Height = height;
        if (german) ui.Control<ComboBox>("LanguageModeComboBox").SelectedIndex = 2;
        ui.Click("NavButton1");
        ui.Control<TextBox>("Line1TextBox").Text = "12345678901234567890";
        ui.Control<TextBox>("Line2TextBox").Text = "ABCDEFGHIJKLMNOPQRST";
        await ui.ClickAndWaitAsync("RenderBoth");
        ui.Click("NavButton0");
        await ui.CaptureAsync($"dashboard-vfd-{(german ? "de" : "en")}-{width}x{height}");

        var first = ui.Control<VfdLine>("PreviewLine1");
        var second = ui.Control<VfdLine>("PreviewLine2");
        var rows = Assert.IsType<StackPanel>(first.Parent);
        var display = Assert.IsType<Border>(rows.Parent);
        Assert.Equal(20, first.Text!.Length);
        Assert.Equal(20, second.Text!.Length);
        var horizontalInset = display.Padding.Left + display.Padding.Right +
            display.BorderThickness.Left + display.BorderThickness.Right;
        var verticalInset = display.Padding.Top + display.Padding.Bottom +
            display.BorderThickness.Top + display.BorderThickness.Bottom;
        Assert.InRange(display.Bounds.Width - Math.Max(first.DesiredSize.Width, second.DesiredSize.Width),
            horizontalInset - 1, horizontalInset + 1);
        Assert.InRange(display.Bounds.Height - first.DesiredSize.Height - second.DesiredSize.Height,
            verticalInset - 1, verticalInset + 1);
        var card = Assert.IsType<Border>(Assert.IsType<StackPanel>(display.Parent).Parent);
        var cardInset = card.Padding.Left + card.Padding.Right +
            card.BorderThickness.Left + card.BorderThickness.Right;
        Assert.InRange(card.Bounds.Width - display.Bounds.Width, cardInset - 1, cardInset + 1);
    }

    [AvaloniaFact]
    public async Task SerialStartupAndCommonActionsUseSavedDeviceAndBrightness()
    {
        await using var ui = await Fixture.OpenAsync(new PortableWorkspace
        {
            TransportMode = "Serial", PortName = "LIS2-TEST-MISSING", BrightnessPercent = 50,
            ProgramCustomGlyphsOnConnect = true,
            CustomGlyphs = Enumerable.Range(1, 8).Select(index => new CustomGlyphSettings
                { Name = "Test " + index, Rows = Enumerable.Repeat((byte)index, 8).ToArray() }).ToList()
        });
        var transport = Assert.Single(ui.SerialTransports);
        Assert.Equal("LIS2-TEST-MISSING", ui.Control<ComboBox>("SettingsPortComboBox").SelectedItem);
        Assert.Equal(Lis2Brightness.Percent50, transport.State.Brightness);
        Assert.Equal((byte)8, transport.State.CustomCharacters[7][0]);
        Assert.DoesNotContain(transport.Writes, IsFanCommand);
        ui.Window.Hide();
        ui.Window.Show();
        await Task.Delay(60);
        Assert.Single(ui.SerialTransports);
        Assert.True(transport.IsOpen);
        ui.Click("NavButton1");
        ui.Control<TextBox>("Line1TextBox").Text = "ÄÖÜ LIS2";
        ui.Control<TextBox>("Line2TextBox").Text = "Selected serial";
        await ui.ClickAndWaitAsync("RenderBoth");
        Assert.StartsWith("ÄÖÜ LIS2", transport.State.Line1);
        Assert.StartsWith("Selected serial", transport.State.Line2);
        Assert.Equal(transport.State.Line1, ui.Control<VfdLine>("PreviewLine1").Text);
        await ui.ClickAndWaitAsync("ClearDisplay");
        Assert.Equal(new string(' ', 20), transport.State.Line1);
        await ui.ClickAndWaitAsync("SendLine1");
        await ui.ClickAndWaitAsync("SendLine2");
        Assert.StartsWith("Selected serial", transport.State.Line2);
        await ui.ClickAndWaitAsync("SendSpecialCharacterTest");
        Assert.StartsWith("ÄÖÜ", transport.State.Line1);
        ui.Control<TextBox>("CharacterRomStartTextBox").Text = "20";
        await ui.ClickAndWaitAsync("SendCharacterRomRange");
        Assert.Contains(transport.Writes, command => command.SequenceEqual(
            Lis2Protocol.WriteRawLine(1, 0, Enumerable.Range(0x20, 20).Select(value => (byte)value).ToArray())));
        await ui.ClickAndWaitAsync("SendGlyphSlot");
        await ui.ClickAndWaitAsync("SendAllGlyphs");
        await ui.ClickAndWaitAsync("Brightness25");
        Assert.Equal(25, ui.Store.Load().BrightnessPercent);
        Assert.Equal(Lis2Brightness.Percent25, transport.State.Brightness);
        ui.Click("NavButton7");
        await ui.ClickAndWaitAsync("SettingsApplyTransport");
        Assert.False(transport.IsOpen);
        Assert.Equal(2, ui.SerialTransports.Count);
        Assert.Equal(Lis2Brightness.Percent25, ui.SerialTransports[1].State.Brightness);
        Assert.All(ui.SerialTransports, item => Assert.DoesNotContain(item.Writes, IsFanCommand));
        ui.Click("NavButton2");
        ui.Click("PageAdd");
        ui.Control<TextBox>("PageLine1").Text = "Serial page preview";
        await ui.ClickAndWaitAsync("PageTest");
        Assert.StartsWith("Serial page preview", ui.SerialTransports[1].State.Line1);
        await ui.ClickAndWaitAsync("PagePlaybackStart");
        Assert.True(ui.Control<Button>("PagePlaybackStop").IsEnabled);
        ui.Click("PagePlaybackStop");
    }

    [AvaloniaFact]
    public async Task TransportChoiceAppliesExplicitlyAndDisconnectClearsConsent()
    {
        await using var ui = await Fixture.OpenAsync(new PortableWorkspace());
        Assert.Empty(ui.SerialTransports);
        ui.Click("NavButton7");
        ui.Control<ComboBox>("TransportModeComboBox").SelectedIndex = 1;
        var ports = ui.Control<ComboBox>("SettingsPortComboBox");
        ports.ItemsSource = new[] { "SERIAL-TEST" };
        ports.SelectedIndex = 0;
        ui.Click("NavButton0");
        await ui.ClickAndWaitAsync("VirtualDemo");
        Assert.Empty(ui.SerialTransports);
        Assert.Equal("Virtual", ui.Store.Load().TransportMode);
        ui.Click("NavButton7");
        await ui.ClickAndWaitAsync("SettingsApplyTransport");
        var serial = Assert.Single(ui.SerialTransports);
        Assert.True(serial.IsOpen);
        Assert.Equal("Serial", ui.Store.Load().TransportMode);
        Assert.Equal("SERIAL-TEST", ui.Store.Load().PortName);
        ui.Control<CheckBox>("DashboardFanConsent").IsChecked = true;
        ui.Control<CheckBox>("FanHardwareConsent").IsChecked = true;
        await ui.ClickAndWaitAsync("SettingsDisconnectTransport");
        Assert.False(serial.IsOpen);
        Assert.False(ui.Control<CheckBox>("DashboardFanConsent").IsChecked);
        Assert.False(ui.Control<CheckBox>("FanHardwareConsent").IsChecked);
        Assert.False(ui.Control<Button>("RenderBoth").IsEnabled);
        ui.Control<ComboBox>("TransportModeComboBox").SelectedIndex = 0;
        await ui.ClickAndWaitAsync("SettingsApplyTransport");
        Assert.Equal("Virtual", ui.Store.Load().TransportMode);
        Assert.Single(ui.SerialTransports);
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(null, true)]
    public async Task HomeAssistantActivationIsIndependentOfStoredToken(bool? enabled, bool expected)
    {
        await using var ui = await Fixture.OpenAsync(new PortableWorkspace { HomeAssistantEnabled = enabled }, true);
        Assert.Equal(expected, ui.Control<CheckBox>("HaEnabled").IsChecked);
        Assert.Equal(expected, ui.Store.Load().HomeAssistantEnabled);
        Assert.Equal("isolated-test-token", ui.TokenStore.Load());
    }

    [AvaloniaFact]
    public async Task WindowsImportUpdatesVisibleSettingsAndKeepsTokenSeparate()
    {
        await using var ui = await Fixture.OpenAsync(new PortableWorkspace
            { TransportMode = "Serial", PortName = "OLD-PORT" }, true);
        ui.Click("NavButton7");
        Assert.True(ui.Control<Button>("ImportWindowsSettings").IsEffectivelyVisible);
        await ui.Window.ImportWindowsSettingsAsync("""
            {"TransportMode":"Serial","PortName":"IMPORTED-PORT","BrightnessPercent":75,
             "Pages":[],"HomeAssistant":{"Enabled":false,"Url":"https://ha.example.test",
             "AccessToken":"NEVER-IMPORT"},"HardwareSensorPreferences":[
             {"Key":"Hardware.cpu.temp","Alias":"CPU","IsFavorite":true}]}
            """);
        Assert.Equal("IMPORTED-PORT", ui.Control<ComboBox>("SettingsPortComboBox").SelectedItem);
        Assert.False(ui.Control<CheckBox>("HaEnabled").IsChecked);
        Assert.Equal("https://ha.example.test", ui.Control<TextBox>("HaUrl").Text);
        Assert.Equal("isolated-test-token", ui.TokenStore.Load());
        var saved = ui.Store.Load();
        Assert.Equal("CPU", Assert.Single(saved.HardwareSensorPreferences).Alias);
        Assert.Equal(75, saved.BrightnessPercent);
        Assert.Empty(saved.Pages);
        Assert.False(ui.SerialTransports[0].IsOpen);
        Assert.Equal(Lis2Brightness.Percent75, ui.SerialTransports[1].State.Brightness);
        var before = File.ReadAllText(ui.StorePath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => ui.Window.ImportWindowsSettingsAsync(
            "{\"TransportMode\":\"Unknown\",\"PortName\":\"BROKEN\"}"));
        Assert.Equal(before, File.ReadAllText(ui.StorePath));
        Assert.Equal("IMPORTED-PORT", ui.Control<ComboBox>("SettingsPortComboBox").SelectedItem);
    }

    [AvaloniaFact]
    public async Task DashboardManualFansRequireFreshConsentAndRestoreSafety()
    {
        var settings = new PortableWorkspace { TransportMode = "Serial", PortName = "FAN-TEST", LanguageMode = "German" };
        settings.Fans.Channels[0].Mode = "Curve";
        settings.Fans.Channels[0].MinimumPercent = 40;
        settings.Fans.Channels[1].MaximumPercent = 70;
        settings.Fans.Channels[2].AllowStop = true;
        await using var ui = await Fixture.OpenAsync(settings);
        var transport = Assert.Single(ui.SerialTransports);
        ui.Control<Slider>("DashboardFan1Slider").Value = 0;
        ui.Control<Slider>("DashboardFan2Slider").Value = 90;
        ui.Control<Slider>("DashboardFan3Slider").Value = 0;
        ui.Control<Slider>("DashboardFan4Slider").Value = 55;
        Assert.False(ui.Control<Button>("DashboardFansApply").IsEnabled);
        Assert.DoesNotContain(transport.Writes, IsFanCommand);
        ui.Control<CheckBox>("DashboardFanConsent").IsChecked = true;
        ui.Window.Width = 950;
        ui.Window.Height = 600;
        await ui.CaptureAsync("port02-serial-consent-950x600");
        ui.Control<ScrollViewer>("DashboardView").Offset = new Vector(0, 1000);
        await ui.CaptureAsync("port02-serial-consent-bottom-950x600");
        await ui.ClickAndWaitAsync("DashboardFansApply");
        var writes = transport.Writes.Where(IsFanCommand).ToArray();
        Assert.Equal(2, writes.Length);
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), writes[0]);
        Assert.Equal(Lis2Protocol.SetFans(40, 70, 0, 55), writes[1]);
        Assert.False(ui.Control<Button>("DashboardFansApply").IsEnabled);
        Assert.False(ui.Control<CheckBox>("DashboardFanConsent").IsChecked);
        Assert.Equal("Curve", ui.Store.Load().Fans.Channels[0].Mode);
        Assert.Equal(0, ui.Store.Load().Fans.Channels[0].FixedPercent);
        ui.Click("NavButton7");
        await ui.ClickAndWaitAsync("SettingsDisconnectTransport");
        Assert.Equal(Lis2Protocol.SetFans(100, 100, 100, 100), transport.Writes[^1]);
    }

    [AvaloniaTheory]
    [InlineData(1305, 893)]
    [InlineData(950, 600)]
    public async Task RenderAffectedViewsAtNormalAndMinimumSize(int width, int height)
    {
        await using var ui = await Fixture.OpenAsync(new PortableWorkspace { LanguageMode = "German" });
        ui.Window.Width = width;
        ui.Window.Height = height;
        await ui.ClickAndWaitAsync("VirtualDemo");
        foreach (var section in new[] { 0, 1, 7 })
        {
            ui.Click("NavButton" + section);
            await ui.CaptureAsync($"port02-{section}-{width}x{height}");
            var scroll = ui.Control<ScrollViewer>(section switch { 0 => "DashboardView", 1 => "DisplayView", _ => "SettingsView" });
            scroll.Offset = new Vector(0, scroll.Extent.Height);
            await ui.CaptureAsync($"port02-{section}-bottom-{width}x{height}");
            scroll.Offset = default;
        }
        ui.Click("NavButton7");
        ui.Control<Button>("SettingsDisconnectTransport").Focus();
        ui.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        ui.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        await Fixture.WaitUntilAsync(() => !ui.Control<Button>("SettingsDisconnectTransport").IsEnabled);
        Assert.Equal("Disconnected", ui.Control<TextBlock>("ConnectionText").Text);
    }

    private static bool IsFanCommand(byte[] bytes) => bytes.Length > 0 && bytes[0] == 0xAE;

    [AvaloniaFact]
    public async Task VirtualManualOutputNeedsNoSerialConsentAndPersistsRequestedValues()
    {
        await using var ui = await Fixture.OpenAsync(new PortableWorkspace());
        Assert.False(ui.Control<CheckBox>("DashboardFanConsent").IsVisible);
        ui.Control<Slider>("DashboardFan1Slider").Value = 10;
        await ui.ClickAndWaitAsync("DashboardFansApply");
        Assert.Equal(30, ui.Control<Slider>("DashboardFan1Slider").Value);
        Assert.Equal(10, ui.Store.Load().Fans.Channels[0].FixedPercent);
        Assert.Empty(ui.SerialTransports);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "lis2-ui-" + Guid.NewGuid().ToString("N"));
        public string StorePath => Path.Combine(_directory, "workspace.json");
        public PortableWorkspaceStore Store { get; }
        public PortableHaTokenStore TokenStore { get; }
        public MainWindow Window { get; private set; } = null!;
        public List<VirtualLis2Transport> SerialTransports { get; } = new();

        private Fixture()
        {
            Store = new PortableWorkspaceStore(StorePath);
            string? token = null;
            TokenStore = new PortableHaTokenStore(_directory, (operation, value) =>
            {
                if (operation == "store") token = value;
                if (operation == "clear") token = null;
                return operation == "lookup" ? token : null;
            });
        }

        public static async Task<Fixture> OpenAsync(PortableWorkspace saved, bool token = false)
        {
            // Erst nach Erstellen der isolierten Datei darf das Fenster seinen Workspace laden.
            var ui = new Fixture();
            saved.LanguageMode = "English";
            await ui.Store.SaveAsync(saved);
            if (token) ui.TokenStore.Save("isolated-test-token");
            ui.Window = new MainWindow(ui.Store, ui.TokenStore, false, _ =>
            {
                var transport = new VirtualLis2Transport();
                ui.SerialTransports.Add(transport);
                return new SerialDisplaySession(transport);
            });
            ui.Window.Show();
            await WaitUntilAsync(() => ui.Control<TextBlock>("ConnectionText").Text?.StartsWith("Connected") == true &&
                ui.Control<Button>("SettingsApplyTransport").IsEnabled);
            return ui;
        }

        public T Control<T>(string name) where T : Control => Window.FindControl<T>(name)!;
        public void Click(string name) => Control<Button>(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        public async Task ClickAndWaitAsync(string name)
        {
            Assert.True(Control<Button>(name).IsEnabled, name + " ist gesperrt.");
            Click(name);
            await WaitUntilAsync(() => Control<Button>("SettingsApplyTransport").IsEnabled);
        }
        public async Task CaptureAsync(string name)
        {
            await Task.Delay(40);
            Window.UpdateLayout();
            using var frame = Window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../TestResults/screenshots"));
            Directory.CreateDirectory(directory);
            frame.Save(Path.Combine(directory, name + ".png"));
        }
        public static async Task WaitUntilAsync(Func<bool> predicate)
        {
            var timeout = DateTime.UtcNow.AddSeconds(10);
            while (!predicate())
            {
                Assert.True(DateTime.UtcNow < timeout, "UI-Aktion hat das Zeitlimit überschritten.");
                await Task.Delay(10);
            }
        }
        public async ValueTask DisposeAsync()
        {
            Window.Close();
            await Window.ShutdownCompletion;
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }
    }
}
