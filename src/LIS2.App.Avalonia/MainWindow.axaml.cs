using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LIS2.App.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        UpdateSection();
    }

    private void OnNavigationChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateSection();
    }

    private void UpdateSection()
    {
        if (this.FindControl<ListBox>("Navigation") is not { SelectedItem: ListBoxItem item })
        {
            return;
        }

        var section = item.Content?.ToString() ?? "Dashboard";
        var title = this.FindControl<TextBlock>("SectionTitle");
        var description = this.FindControl<TextBlock>("SectionDescription");

        if (title is not null)
        {
            title.Text = section;
        }

        if (description is not null)
        {
            description.Text = $"{section} is not connected to the LIS2 runtime yet.";
        }
    }
}
