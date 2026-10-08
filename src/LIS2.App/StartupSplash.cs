using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace LIS2.App;

// Separate STA dispatcher keeps the splash responsive while the main UI initializes.
internal sealed class StartupSplash : IDisposable
{
    private readonly ManualResetEventSlim _ready = new(false);
    private Thread? _thread;
    private Dispatcher? _dispatcher;
    private Window? _window;
    private TextBlock? _status;

    public void Show()
    {
        _thread = new Thread(() =>
        {
            try
            {
                _dispatcher = Dispatcher.CurrentDispatcher;
                var panel = new StackPanel { Margin = new Thickness(28) };
                panel.Children.Add(new TextBlock
                {
                    Text = "LIS2 Control Center",
                    FontSize = 22,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = System.Windows.Media.Brushes.White,
                    Margin = new Thickness(0, 0, 0, 18)
                });
                _status = new TextBlock
                {
                    Text = "Control Center is loading...",
                    FontSize = 15,
                    Foreground = System.Windows.Media.Brushes.White,
                    Margin = new Thickness(0, 0, 0, 14)
                };
                panel.Children.Add(_status);
                panel.Children.Add(new System.Windows.Controls.ProgressBar
                {
                    IsIndeterminate = true,
                    Height = 6,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(77, 198, 142))
                });
                _window = new Window
                {
                    Title = "LIS2 Control Center",
                    Width = 390,
                    Height = 170,
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(27, 45, 72)),
                    Content = panel,
                    Topmost = true
                };
                _window.Show();
                _ready.Set();
                Dispatcher.Run();
            }
            finally
            {
                _ready.Set();
            }
        }) { IsBackground = true, Name = "LIS2 startup splash" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _ready.Wait(TimeSpan.FromSeconds(2));
    }

    public void UpdateLanguage()
    {
        var translated = LocalizationService.Translate("Control Center is loading...");
        _dispatcher?.BeginInvoke(() => { if (_status is not null) _status.Text = translated; });
    }

    public void Dispose()
    {
        var dispatcher = _dispatcher;
        if (dispatcher is not null && !dispatcher.HasShutdownStarted)
            dispatcher.BeginInvoke(() => { _window?.Close(); dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); });
    }
}
