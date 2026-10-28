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
    internal event Action? UiLanguageChanged;

    private async System.Threading.Tasks.Task OpenHelpAsync()
    {
        var dialog = new Window
        {
            Name = "HelpDialog",
            Icon = Icon, Width = 1080, Height = 780, MinWidth = 840, MinHeight = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var title = new TextBlock { FontSize = 25, FontWeight = FontWeight.SemiBold };
        var close = new Button { Name = "HelpClose", MinWidth = 90 };
        close.Click += (_, _) => dialog.Close();
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 0, 0, 14) };
        var heading = new StackPanel { Spacing = 5 };
        heading.Children.Add(title);
        header.Children.Add(heading);
        Grid.SetColumn(close, 1);
        header.Children.Add(close);

        var sectionList = new ListBox { Name = "HelpSections", Width = 235 };
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
                Text = HelpReferenceLocalization.Translate(HelpReferenceCatalog.Sections[index], CurrentUiLanguage()),
                FontSize = 24, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap
            };
            content.Children.Add(sectionTitle);
            foreach (var (sectionHeading, text, isExample) in HelpReferenceCatalog.GetSection(index,
                         PagePreviewValues().Keys, _glyphs.Glyphs.Select(g => g.Name), CurrentUiLanguage()))
            {
                if (sectionHeading != sectionTitle.Text)
                    content.Children.Add(new TextBlock
                    {
                        Text = sectionHeading, FontSize = 17, TextWrapping = TextWrapping.Wrap,
                        FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 12, 0, 0)
                    });
                content.Children.Add(new TextBlock
                {
                    Text = text, TextWrapping = TextWrapping.Wrap,
                    // Eingebettete Vorlagen ändern nicht die Schrift des erklärenden Absatzes.
                    FontFamily = isExample ? new FontFamily("Consolas,DejaVu Sans Mono,Liberation Mono") : FontFamily.Default
                });
            }
            scroll.Offset = new Vector(0, 0);
        }
        void ApplyLanguage()
        {
            var selected = HelpReferenceCatalog.ClampSection(sectionList.SelectedIndex);
            var language = CurrentUiLanguage();
            dialog.Title = title.Text = HelpReferenceLocalization.Translate("Help", language);
            close.Content = HelpReferenceLocalization.Translate("Close", language);
            sectionList.ItemsSource = HelpReferenceCatalog.Sections
                .Select(section => HelpReferenceLocalization.Translate(section, language)).ToArray();
            sectionList.SelectedIndex = selected;
            Render();
        }
        ApplyLanguage();
        sectionList.SelectionChanged += (_, _) => Render();
        sectionList.SelectedIndex = HelpReferenceCatalog.ClampSection(_selectedSection);
        UiLanguageChanged += ApplyLanguage;
        try { await dialog.ShowDialog(this); }
        finally { UiLanguageChanged -= ApplyLanguage; }
    }

    private async System.Threading.Tasks.Task OpenAboutAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? assembly.GetName().Version?.ToString() ?? "unknown";
        var dataDirectory = System.IO.Path.GetDirectoryName(_workspaceStore.FilePath) ?? _workspaceStore.FilePath;
        var dialog = new Window
        {
            Name = "AboutDialog",
            Title = SettingsTranslate("About") + " LIS2 Control Center",
            Icon = Icon, Width = 640, MinWidth = 450, Height = 520,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        var content = new StackPanel { Spacing = 12, Margin = new Thickness(24) };
        content.Children.Add(new TextBlock { Text = "LIS2 Control Center", FontSize = 24, FontWeight = FontWeight.SemiBold });
        content.Children.Add(new TextBlock { Text = UiFormat($"Version {version}"), TextWrapping = TextWrapping.Wrap });
        foreach (var (label, value) in new[]
        {
            ("Build", assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "unknown"),
            ("Runtime", RuntimeInformation.FrameworkDescription),
            ("Avalonia", typeof(Application).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? typeof(Application).Assembly.GetName().Version!.ToString()),
            ("License", "MIT"),
            ("Architecture", RuntimeInformation.ProcessArchitecture.ToString()),
            ("Operating system", RuntimeInformation.OSDescription),
            ("Application data", dataDirectory)
        })
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("140,*") };
            row.Children.Add(new TextBlock { Text = BrowserText(label), FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
            var valueText = new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap };
            Grid.SetColumn(valueText, 1); row.Children.Add(valueText);
            content.Children.Add(row);
        }
        var buttons = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 12 };
        var github = new Button { Content = "GitHub", MinWidth = 100 };
        github.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo("https://github.com/Eitschman/lis2-control-center") { UseShellExecute = true }); }
            catch (Exception ex) { github.Content = "GitHub: " + ex.Message; }
        };
        buttons.Children.Add(github);
        var licenseStatus = new TextBlock { Name = "AboutLicenseStatus", TextWrapping = TextWrapping.Wrap };
        var licenses = new Button { Name = "AboutLicenses", Content = BrowserText("Licenses"), MinWidth = 100 };
        licenses.Click += (_, _) =>
        {
            // Die unveränderten Lizenztexte stammen aus dem tatsächlichen Paket, nicht aus einer zweiten UI-Liste.
            var directory = System.IO.Path.Combine(AppContext.BaseDirectory, "licenses");
            if (!System.IO.Directory.Exists(directory))
            {
                licenseStatus.Text = BrowserText("License files are not installed.");
                return;
            }
            try { Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true }); }
            catch (Exception ex) { licenseStatus.Text = BrowserText("Licenses") + ": " + ex.Message; }
        };
        Grid.SetColumn(licenses, 1); buttons.Children.Add(licenses);
        var close = new Button { Content = BrowserText("Close"), MinWidth = 100 };
        close.Click += (_, _) => dialog.Close();
        Grid.SetColumn(close, 2); buttons.Children.Add(close);
        content.Children.Add(buttons);
        content.Children.Add(licenseStatus);
        dialog.Content = new ScrollViewer { Content = content };
        await dialog.ShowDialog(this);
    }
}
