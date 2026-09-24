using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace LIS2.App;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        Icon = TrayIconService.CreateWindowIcon();

        VersionText.Text = LocalizationService.Format("Version {0}", AppInfo.Version);
        BuildText.Text = AppInfo.BuildConfiguration;
        RuntimeText.Text = AppInfo.Runtime;
        ArchitectureText.Text = AppInfo.Architecture;
        OperatingSystemText.Text = AppInfo.OperatingSystem;
        DataDirectoryText.Text = AppInfo.DataDirectory;

        var icon = TrayIconService.CreateWindowIcon();
        if (icon is not null)
            AppIconImage.Source = icon;

        LocalizationService.ApplyTo(this);
    }

    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = AppInfo.RepositoryUrl,
            UseShellExecute = true
        });
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
