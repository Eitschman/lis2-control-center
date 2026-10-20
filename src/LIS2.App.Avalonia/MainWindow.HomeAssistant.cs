using Avalonia.Controls;
using Avalonia.Threading;
using LIS2.Sources;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private PeriodicRuntimeLoop _haUiTimer = null!;
    private bool _haActionBusy;
    private sealed record HaEntityRow(string EntityId, string FriendlyName, string State, string TemplateKey, string Favorite);


    private void InitializeHomeAssistant()
    {
        this.FindControl<Button>("HaConnect")!.Click += async (_, _) =>
            await RunHaActionAsync(async () =>
            {
                var enabled = this.FindControl<CheckBox>("HaEnabled")!.IsChecked == true;
                var token = this.FindControl<TextBox>("HaToken")!.Text;
                await _pageSources.ConfigureHomeAssistantAsync(
                    enabled ? this.FindControl<TextBox>("HaUrl")!.Text : null,
                    enabled ? token : null);
                if (enabled && !string.IsNullOrWhiteSpace(token))
                    _haTokenStore.Save(token);
                else if (!enabled)
                    _haTokenStore.Delete();
                await SavePortableWorkspaceAsync(true);
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
        this.FindControl<ComboBox>("HaEntityPicker")!.SelectionChanged += (_, _) => RefreshHaSelection();
        this.FindControl<ListBox>("HaEntityList")!.SelectionChanged += (_, _) =>
        {
            if (_updatingHaSelection) return;
            if (this.FindControl<ListBox>("HaEntityList")!.SelectedItem is not HaEntityRow row) return;
            var picker = this.FindControl<ComboBox>("HaEntityPicker")!;
            if (!string.Equals(picker.SelectedItem as string, row.EntityId, StringComparison.OrdinalIgnoreCase))
                picker.SelectedItem = row.EntityId;
        };
        this.FindControl<ComboBox>("HaDomainFilter")!.SelectionChanged += (_, _) => RefreshHaEntities();
        this.FindControl<Button>("HaSaveEntity")!.Click += async (_, _) =>
        {
            if (SelectedHaEntityId() is not string id) return;
            var preference = _haPreferences.FirstOrDefault(p => string.Equals(p.EntityId, id, StringComparison.OrdinalIgnoreCase));
            if (preference is null)
            {
                preference = new PortableHaEntityPreference { EntityId = id };
                _haPreferences.Add(preference);
            }
            preference.Alias = this.FindControl<TextBox>("HaAlias")!.Text?.Trim() ?? string.Empty;
            preference.IsFavorite = this.FindControl<CheckBox>("HaFavorite")!.IsChecked == true;
            await SavePortableWorkspaceAsync();
            RefreshHaEntities();
        };
        this.FindControl<Button>("HaCopyKey")!.Click += async (_, _) =>
        {
            if (SelectedHaEntityId() is string id)
                await CopyHaKeyAsync(HaEntityKey(id));
        };
        this.FindControl<Button>("HaCopyAttributeKey")!.Click += async (_, _) =>
        {
            if (SelectedHaAttribute() is string name && SelectedHaEntityId() is string id)
                await CopyHaKeyAsync(HaAttributeKey(id, name));
        };
        this.FindControl<Button>("HaCreatePage")!.Click += async (_, _) => await CreateHaPageAsync(false);
        this.FindControl<Button>("HaCreateAttributePage")!.Click += async (_, _) => await CreateHaPageAsync(true);
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

    private bool _updatingHaSelection;

    private string? SelectedHaEntityId() =>
        this.FindControl<ComboBox>("HaEntityPicker")!.SelectedItem as string;

    private string? SelectedHaAttribute() =>
        this.FindControl<ListBox>("HaAttributes")!.SelectedItem as string;

    private string HaEntityKey(string id)
    {
        var alias = _haPreferences.FirstOrDefault(p => string.Equals(p.EntityId, id, StringComparison.OrdinalIgnoreCase))?.Alias;
        var normalized = NormalizeHaAlias(alias);
        return "{HA." + (normalized.Length > 0 ? normalized : id) + "}";
    }

    private static string NormalizeHaAlias(string? alias) =>
        new string((alias ?? "").Trim().Select(c => char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '_').ToArray())
            .Trim('_').Replace("__", "_", StringComparison.Ordinal);

    private string HaAttributeKey(string id, string name) =>
        HaEntityKey(id).TrimEnd('}') + "." + name + "}";

    private async Task CopyHaKeyAsync(string key)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null) await clipboard.SetTextAsync(key);
    }

    private void RefreshHaEntities()
    {
        var ha = _pageSources.HomeAssistant;
        var picker = this.FindControl<ComboBox>("HaEntityPicker")!;
        var previous = SelectedHaEntityId();
        var domains = new[] { "All domains" }.Concat(ha.Entities.Select(e => e.Domain).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(d => d, StringComparer.OrdinalIgnoreCase)).ToArray();
        var domainPicker = this.FindControl<ComboBox>("HaDomainFilter")!;
        var selectedDomain = domainPicker.SelectedItem as string ?? "All domains";
        if (!domains.SequenceEqual(domainPicker.ItemsSource as string[] ?? Array.Empty<string>()))
        {
            _updatingHaSelection = true;
            domainPicker.ItemsSource = domains;
            domainPicker.SelectedItem = domains.Contains(selectedDomain) ? selectedDomain : "All domains";
            _updatingHaSelection = false;
        }
        var filter = this.FindControl<TextBox>("HaEntityFilter")!.Text?.Trim() ?? "";
        var domain = domainPicker.SelectedItem as string ?? "All domains";
        var entities = ha.Entities
            .Where(e => (domain == "All domains" || string.Equals(e.Domain, domain, StringComparison.OrdinalIgnoreCase))
                && (e.EntityId.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || e.FriendlyName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || (_haPreferences.FirstOrDefault(p => string.Equals(p.EntityId, e.EntityId, StringComparison.OrdinalIgnoreCase))?.Alias?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)))
            .OrderByDescending(e => _haPreferences.Any(p => string.Equals(p.EntityId, e.EntityId, StringComparison.OrdinalIgnoreCase) && p.IsFavorite))
            .ThenBy(e => e.EntityId, StringComparer.OrdinalIgnoreCase)
            .Select(e => e.EntityId).ToArray();
        if (!entities.SequenceEqual(picker.ItemsSource as string[] ?? Array.Empty<string>()))
        {
            _updatingHaSelection = true;
            picker.ItemsSource = entities;
            picker.SelectedItem = previous is not null && entities.Contains(previous) ? previous : entities.FirstOrDefault();
            _updatingHaSelection = false;
            RefreshHaSelection();
        }
        var rows = entities.Select(id =>
        {
            var entity = ha.Entities.First(e => string.Equals(e.EntityId, id, StringComparison.OrdinalIgnoreCase));
            var favorite = _haPreferences.Any(p => string.Equals(p.EntityId, id, StringComparison.OrdinalIgnoreCase) && p.IsFavorite);
            return new HaEntityRow(id, entity.FriendlyName, entity.State + " " + entity.Unit,
                HaEntityKey(id), favorite ? "★" : "");
        }).ToArray();
        var entityList = this.FindControl<ListBox>("HaEntityList")!;
        _updatingHaSelection = true;
        try
        {
            entityList.ItemsSource = rows;
            entityList.SelectedItem = rows.FirstOrDefault(row =>
                string.Equals(row.EntityId, picker.SelectedItem as string, StringComparison.OrdinalIgnoreCase));
        }
        finally { _updatingHaSelection = false; }
        this.FindControl<TextBlock>("HaStatus")!.Text = ha.IsConnected
            ? $"Home Assistant connected ({ha.HomeAssistantVersion ?? "unknown version"}), {ha.Entities.Count} entities, read-only."
            : ha.LastError is { Length: > 0 } error ? $"Home Assistant disconnected: {error}"
            : ha.IsConfigured ? "Home Assistant connecting..." : "Home Assistant not configured";
    }

    private void RefreshHaSelection()
    {
        if (_updatingHaSelection) return;
        var id = SelectedHaEntityId();
        var listView = this.FindControl<ListBox>("HaEntityList")!;
        if (listView.SelectedItem is not HaEntityRow selectedRow ||
            !string.Equals(selectedRow.EntityId, id, StringComparison.OrdinalIgnoreCase))
        {
            _updatingHaSelection = true;
            try
            {
                listView.SelectedItem = (listView.ItemsSource as IEnumerable<HaEntityRow>)?
                    .FirstOrDefault(row => string.Equals(row.EntityId, id, StringComparison.OrdinalIgnoreCase));
            }
            finally { _updatingHaSelection = false; }
        }
        var entity = _pageSources.HomeAssistant.Entities.FirstOrDefault(e => e.EntityId == id);
        var preference = _haPreferences.FirstOrDefault(p => string.Equals(p.EntityId, id, StringComparison.OrdinalIgnoreCase));
        this.FindControl<TextBlock>("HaSelectedEntity")!.Text = entity is null
            ? "Select an entity above." : $"{entity.FriendlyName} ({entity.EntityId}) — {entity.State} {entity.Unit}";
        this.FindControl<TextBox>("HaAlias")!.Text = preference?.Alias ?? "";
        this.FindControl<CheckBox>("HaFavorite")!.IsChecked = preference?.IsFavorite == true;
        var attributes = entity?.Attributes.Keys.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();
        var list = this.FindControl<ListBox>("HaAttributes")!;
        var previous = list.SelectedItem as string;
        if (!attributes.SequenceEqual(list.ItemsSource as string[] ?? Array.Empty<string>()))
        {
            list.ItemsSource = attributes;
            list.SelectedItem = previous is not null && attributes.Contains(previous) ? previous : attributes.FirstOrDefault();
        }
    }

    private async Task CreateHaPageAsync(bool attribute)
    {
        var id = SelectedHaEntityId();
        var entity = _pageSources.HomeAssistant.Entities.FirstOrDefault(e => e.EntityId == id);
        if (entity is null) return;
        var attributeName = attribute ? SelectedHaAttribute() : null;
        if (attribute && attributeName is null) return;
        var preference = _haPreferences.FirstOrDefault(p => string.Equals(p.EntityId, id, StringComparison.OrdinalIgnoreCase));
        var displayName = string.IsNullOrWhiteSpace(preference?.Alias) ? entity.FriendlyName : preference.Alias;
        var key = attributeName is null ? HaEntityKey(entity.EntityId) : HaAttributeKey(entity.EntityId, attributeName);
        await AddSourcePageAsync(page =>
        {
            page.Name = attributeName is null ? displayName : displayName + " · " + attributeName;
            page.Line1Template = displayName;
            page.Line2Template = key.TrimEnd('}') + "|fallback:--}" + (attributeName is null ? entity.Unit : "");
            page.DurationSeconds = 5;
            page.Line1OverflowMode = "PingPong";
            page.Line2OverflowMode = "Truncate";
        });
    }

    private void StopHomeAssistantUi() => _haUiTimer.Stop();
}
