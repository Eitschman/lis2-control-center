using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private async System.Threading.Tasks.Task OpenHelpAsync()
    {
        var dialog = new Window
        {
            Title = SettingsTranslate("Help") + " — LIS2 Control Center",
            Icon = Icon, Width = 1080, Height = 780, MinWidth = 840, MinHeight = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var title = new TextBlock { Text = SettingsTranslate("Help") + " & reference", FontSize = 25, FontWeight = FontWeight.SemiBold };
        var subtitle = new TextBlock
        {
            Text = CurrentUiLanguage() == "de"
                ? "Anleitung, Konfigurationsreferenz und Beispiele für Displayvariablen."
                : "Instructions, configuration reference and display-variable examples.",
            TextWrapping = TextWrapping.Wrap
        };
        var close = new Button { Content = BrowserText("Close"), MinWidth = 90 };
        close.Click += (_, _) => dialog.Close();
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 0, 0, 14) };
        var heading = new StackPanel { Spacing = 5 };
        heading.Children.Add(title);
        heading.Children.Add(subtitle);
        header.Children.Add(heading);
        Grid.SetColumn(close, 1);
        header.Children.Add(close);

        var sectionList = new ListBox { Width = 235, ItemsSource = HelpReferenceCatalog.Sections.Select(SettingsTranslate).ToArray() };
        var content = new StackPanel { Spacing = 10, Margin = new Thickness(18) };
        var scroll = new ScrollViewer { Content = content, HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("235,8,*") };
        Grid.SetColumn(sectionList, 0); body.Children.Add(sectionList);
        var splitter = new GridSplitter { Width = 6, HorizontalAlignment = HorizontalAlignment.Center };
        Grid.SetColumn(splitter, 1); body.Children.Add(splitter);
        Grid.SetColumn(scroll, 2); body.Children.Add(scroll);

        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), Margin = new Thickness(18) };
        layout.Children.Add(header);
        Grid.SetRow(body, 1); layout.Children.Add(body);
        dialog.Content = layout;

        void Render()
        {
            content.Children.Clear();
            var index = HelpReferenceCatalog.ClampSection(sectionList.SelectedIndex);
            var sectionTitle = new TextBlock
            {
                Text = SettingsTranslate(HelpReferenceCatalog.Sections[index]),
                FontSize = 24, FontWeight = FontWeight.SemiBold
            };
            content.Children.Add(sectionTitle);
            foreach (var (sectionHeading, text) in HelpReferenceCatalog.GetSection(index,
                         PagePreviewValues().Keys, _glyphs.Glyphs.Select(g => g.Name)))
            {
                content.Children.Add(new TextBlock
                {
                    Text = BrowserText(sectionHeading), FontSize = 17,
                    FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 12, 0, 0)
                });
                content.Children.Add(new TextBlock
                {
                    Text = BrowserText(text), TextWrapping = TextWrapping.Wrap,
                    FontFamily = text.Contains('{') ? FontFamily.Parse("Consolas") : FontFamily.Default
                });
            }
            scroll.Offset = new Vector(0, 0);
        }
        sectionList.SelectionChanged += (_, _) => Render();
        sectionList.SelectedIndex = HelpReferenceCatalog.ClampSection(_selectedSection);
        await dialog.ShowDialog(this);
    }

    private async System.Threading.Tasks.Task OpenAboutAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? assembly.GetName().Version?.ToString() ?? "unknown";
        var dataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dialog = new Window
        {
            Title = SettingsTranslate("About") + " LIS2 Control Center",
            Icon = Icon, Width = 560, MinWidth = 450, Height = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        var content = new StackPanel { Spacing = 12, Margin = new Thickness(24) };
        content.Children.Add(new TextBlock { Text = "LIS2 Control Center", FontSize = 24, FontWeight = FontWeight.SemiBold });
        content.Children.Add(new TextBlock { Text = "Version " + version });
        foreach (var (label, value) in new[]
        {
            ("Build", assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "unknown"),
            ("Runtime", RuntimeInformation.FrameworkDescription),
            ("Architecture", RuntimeInformation.ProcessArchitecture.ToString()),
            ("Operating system", RuntimeInformation.OSDescription),
            ("Application data", dataDirectory)
        })
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("140,*") };
            row.Children.Add(new TextBlock { Text = BrowserText(label), FontWeight = FontWeight.SemiBold });
            var valueText = new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap };
            Grid.SetColumn(valueText, 1); row.Children.Add(valueText);
            content.Children.Add(row);
        }
        var buttons = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        var github = new Button { Content = "GitHub", MinWidth = 100 };
        github.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo("https://github.com/Eitschman/lis2-control-center") { UseShellExecute = true }); }
            catch (Exception ex) { github.Content = "GitHub: " + ex.Message; }
        };
        buttons.Children.Add(github);
        var close = new Button { Content = BrowserText("Close"), MinWidth = 100 };
        close.Click += (_, _) => dialog.Close();
        Grid.SetColumn(close, 1); buttons.Children.Add(close);
        content.Children.Add(buttons);
        dialog.Content = new ScrollViewer { Content = content };
        await dialog.ShowDialog(this);
    }
}
