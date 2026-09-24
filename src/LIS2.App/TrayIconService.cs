using System.Drawing;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

namespace LIS2.App;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _showItem;
    private readonly Forms.ToolStripMenuItem _exitItem;
    private readonly Icon _icon;
    private string? _lastStatus;

    public TrayIconService()
    {
        var menu = new Forms.ContextMenuStrip();

        _showItem = new Forms.ToolStripMenuItem();
        _showItem.Click += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);

        _exitItem = new Forms.ToolStripMenuItem();
        _exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        menu.Items.Add(_showItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_exitItem);

        _icon = CreateLis2Icon();

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "LIS2 Control Center",
            Icon = _icon,
            ContextMenuStrip = menu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) =>
            ShowRequested?.Invoke(this, EventArgs.Empty);

        LocalizationService.LanguageChanged += LocalizationService_LanguageChanged;
        ApplyLocalization();
    }

    public event EventHandler? ShowRequested;
    public event EventHandler? ExitRequested;

    public void SetStatus(string status)
    {
        _lastStatus = status;
        UpdateTooltip();
    }

    public void ApplyLocalization()
    {
        _showItem.Text = LocalizationService.Translate("Open LIS2 Control Center");
        _exitItem.Text = LocalizationService.Translate("Exit");
        UpdateTooltip();
    }

    private void LocalizationService_LanguageChanged(object? sender, EventArgs e) =>
        ApplyLocalization();

    private void UpdateTooltip()
    {
        var status = string.IsNullOrWhiteSpace(_lastStatus)
            ? null
            : LocalizationService.Translate(_lastStatus);

        var text = status is null
            ? "LIS2 Control Center"
            : $"LIS2 Control Center - {status}";

        _notifyIcon.Text = text.Length <= 63
            ? text
            : text[..63];
    }

    private static Icon CreateLis2Icon()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var background = new SolidBrush(Color.FromArgb(23, 27, 32));
        using var accent = new SolidBrush(Color.FromArgb(105, 238, 138));
        using var border = new Pen(Color.FromArgb(70, 82, 92), 1.5f);
        using var font = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Pixel);

        graphics.FillEllipse(background, 1, 1, 30, 30);
        graphics.DrawEllipse(border, 1.5f, 1.5f, 29, 29);

        var text = "L2";
        var size = graphics.MeasureString(text, font);
        graphics.DrawString(
            text,
            font,
            accent,
            (32 - size.Width) / 2f,
            (32 - size.Height) / 2f - 0.5f);

        var handle = bitmap.GetHicon();

        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    public void Dispose()
    {
        LocalizationService.LanguageChanged -= LocalizationService_LanguageChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon.Dispose();
    }
}
