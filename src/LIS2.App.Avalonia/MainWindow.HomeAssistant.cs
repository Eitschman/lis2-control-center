using Avalonia.Controls;
using Avalonia.Threading;
using LIS2.Sources;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private PeriodicRuntimeLoop _haUiTimer = null!;
    private bool _haActionBusy;

    private void InitializeHomeAssistant()
    {
        this.FindControl<Button>("HaConnect")!.Click += async (_, _) =>
            await RunHaActionAsync(async () =>
            {
                await _pageSources.ConfigureHomeAssistantAsync(
                    this.FindControl<CheckBox>("HaEnabled")!.IsChecked == true ? this.FindControl<TextBox>("HaUrl")!.Text : null,
                    this.FindControl<CheckBox>("HaEnabled")!.IsChecked == true ? this.FindControl<TextBox>("HaToken")!.Text : null);
                this.FindControl<TextBox>("HaToken")!.Text = string.Empty;
                await SavePortableWorkspaceAsync();
            });
        this.FindControl<Button>("HaTest")!.Click += async (_, _) =>
            await RunHaActionAsync(async () =>
            {
                var probe = new HomeAssistantDataSource();
                try
                {
                    probe.Configure(this.FindControl<TextBox>("HaUrl")!.Text,
                        this.FindControl<TextBox>("HaToken")!.Text);
                    await probe.TestConnectionAsync();
                    this.FindControl<TextBlock>("HaStatus")!.Text = "Home Assistant authentication succeeded.";
                }
                finally { await probe.DisposeAsync(); }
            });
        this.FindControl<TextBox>("HaEntityFilter")!.TextChanged += (_, _) => RefreshHaEntities();
        this.FindControl<Button>("HaInsertLine1")!.Click += (_, _) => InsertHaEntity("PageLine1");
        this.FindControl<Button>("HaInsertLine2")!.Click += (_, _) => InsertHaEntity("PageLine2");
        _haUiTimer = new PeriodicRuntimeLoop(TimeSpan.FromSeconds(2),
            cancellationToken => UiRuntimeDispatch.RunAsync(() =>
            {
                RefreshHaEntities();
                if (_virtualSequence?.IsRunning == true) RefreshPagePreview();
                return Task.CompletedTask;
            }, cancellationToken));
        _haUiTimer.Start();
        RefreshHaEntities();
    }

    private async Task RunHaActionAsync(Func<Task> action)
    {
        if (_haActionBusy) return;
        _haActionBusy = true;
        try
        {
            await action();
            RefreshHaEntities();
            RefreshPageVariables();
        }
        catch (Exception ex)
        {
            this.FindControl<TextBlock>("HaStatus")!.Text = $"Home Assistant error: {ex.Message}";
        }
        finally { _haActionBusy = false; }
    }

    private void RefreshHaEntities()
    {
        var ha = _pageSources.HomeAssistant;
        var picker = this.FindControl<ComboBox>("HaEntityPicker")!;
        var previous = picker.SelectedItem as string;
        var filter = this.FindControl<TextBox>("HaEntityFilter")!.Text?.Trim() ?? string.Empty;
        var entities = ha.Entities
            .Where(e => e.EntityId.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        e.FriendlyName.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.EntityId).ToArray();
        if (!entities.SequenceEqual(picker.ItemsSource as string[] ?? Array.Empty<string>()))
        {
            picker.ItemsSource = entities;
            picker.SelectedItem = previous is not null && entities.Contains(previous)
                ? previous : entities.FirstOrDefault();
        }
        this.FindControl<TextBlock>("HaStatus")!.Text = ha.IsConnected
            ? $"Home Assistant connected ({ha.HomeAssistantVersion ?? "unknown version"}), {ha.Entities.Count} entities, read-only."
            : ha.LastError is { Length: > 0 } error
                ? $"Home Assistant disconnected: {error}"
                : ha.IsConfigured ? "Home Assistant connecting..." : "Home Assistant not configured";
    }

    private void InsertHaEntity(string target)
    {
        if (this.FindControl<ComboBox>("HaEntityPicker")!.SelectedItem is not string id) return;
        var input = this.FindControl<TextBox>(target)!;
        var original = input.Text ?? string.Empty;
        var caret = Math.Clamp(input.CaretIndex, 0, original.Length);
        var token = "{HA." + id + "}";
        input.Text = original.Insert(caret, token);
        input.CaretIndex = caret + token.Length;
        input.Focus();
        RefreshPagePreview();
    }

    private void StopHomeAssistantUi() => _haUiTimer.Stop();
}
