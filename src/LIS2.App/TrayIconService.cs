using System.Drawing;
using Forms = System.Windows.Forms;

namespace LIS2.App;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;

    public TrayIconService()
    {
        var menu = new Forms.ContextMenuStrip();

        var showItem = new Forms.ToolStripMenuItem("Open LIS2 Control Center");
        showItem.Click += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);

        var exitItem = new Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        menu.Items.Add(showItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "LIS2 Control Center",
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) =>
            ShowRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? ShowRequested;
    public event EventHandler? ExitRequested;

    public void SetStatus(string status)
    {
        var text = string.IsNullOrWhiteSpace(status)
            ? "LIS2 Control Center"
            : $"LIS2 Control Center - {status}";

        _notifyIcon.Text = text.Length <= 63
            ? text
            : text[..63];
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
