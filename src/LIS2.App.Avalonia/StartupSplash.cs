using Avalonia.Controls;
using Avalonia.Layout;

namespace LIS2.App.Avalonia;

/// <summary>Zeigt den Start an, bevor Quellen und Hauptfenster initialisiert werden.</summary>
internal sealed class StartupSplash : Window
{
    internal StartupSplash()
    {
        Title = "LIS2 Control Center";
        Width = 420;
        Height = 150;
        CanResize = false;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        SystemDecorations = SystemDecorations.None;
        Content = new TextBlock
        {
            Text = "LIS2 Control Center", FontSize = 28,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }
}
