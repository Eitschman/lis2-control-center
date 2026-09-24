using System.Drawing;
using System.IO;
using Forms = System.Windows.Forms;

namespace LIS2.App;

public sealed class TrayIconService : IDisposable
{
    private const string Lis2IconBase64 =
        "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";

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

        _icon = LoadEmbeddedLis2Icon();

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

    private static Icon LoadEmbeddedLis2Icon()
    {
        var bytes = Convert.FromBase64String(Lis2IconBase64);

        using var stream = new MemoryStream(bytes, writable: false);
        using var icon = new Icon(stream);

        return (Icon)icon.Clone();
    }

    public void Dispose()
    {
        LocalizationService.LanguageChanged -= LocalizationService_LanguageChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon.Dispose();
    }
}
