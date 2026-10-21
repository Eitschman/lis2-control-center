using Avalonia.Controls;
using Avalonia.Threading;
using LIS2.Sources;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private PeriodicRuntimeLoop _haUiTimer = null!;
    private bool _haActionBusy;
    private UiMessage? _haActionMessage;
    private sealed record HaEntityRow(string EntityId, string FriendlyName, string State, string TemplateKey, string Favorite);
    private sealed record HaAttributeRow(string Name, string Value);


    private void InitializeHomeAssistant()
    {
        this.FindControl<CheckBox>("HaLocalTokenStorage")!.IsVisible = OperatingSystem.IsLinux();
        this.FindControl<Button>("HaConnect")!.Click += async (_, _) =>
            await RunHaActionAsync(async () =>
            {
                var enabled = this.FindControl<CheckBox>("HaEnabled")!.IsChecked == true;
                var token = this.FindControl<TextBox>("HaToken")!.Text;
                var url = this.FindControl<TextBox>("HaUrl")!.Text;
                if (enabled && (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || string.IsNullOrWhiteSpace(token)))
                    throw new InvalidOperationException("Home Assistant URL and access token are required.");
                _haTokenStore.UseLocalStorage = OperatingSystem.IsLinux() && this.FindControl<CheckBox>("HaLocalTokenStorage")!.IsChecked == true;
                // Vor einer neuen Verbindung muss die gewählte Speicherung erfolgreich sein.
                if (enabled && !string.IsNullOrWhiteSpace(token))
                    await Task.Run(() => _haTokenStore.Save(token.Trim()));
                else if (!enabled)
                {
                    await _pageSources.ConfigureHomeAssistantAsync(null, null);
                    await SavePortableWorkspaceAsync(true);
                    await Task.Run(_haTokenStore.Delete);
                    this.FindControl<TextBox>("HaToken")!.Text = "";
                    return;
                }
                await SavePortableWorkspaceAsync(true);
                if (enabled) await _pageSources.ConfigureHomeAssistantAsync(url, token);
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
                    _haActionMessage = "Home Assistant authentication succeeded.";
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
            ApplyHaAliases();
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
        if (_startBackgroundSources) _haUiTimer.Start();
        RefreshHaEntities();
    }

    private async Task RunHaActionAsync(Func<Task> action)
    {
        if (_haActionBusy) return;
        _haActionBusy = true;
        _haActionMessage = null;
        this.FindControl<Button>("HaConnect")!.IsEnabled = false;
        this.FindControl<Button>("HaTest")!.IsEnabled = false;
        foreach (var name in new[] { "HaEnabled", "HaLocalTokenStorage", "HaUrl", "HaToken" })
            this.FindControl<Control>(name)!.IsEnabled = false;
        RefreshHaActionMessage();
        try
        {
            await action();
            RefreshPageVariables();
        }
        catch (Exception ex)
        {
            _haActionMessage = $"Home Assistant error: {(UiMessage)ex.Message}";
        }
        finally
        {
            _haActionBusy = false;
            this.FindControl<Button>("HaConnect")!.IsEnabled = true;
            this.FindControl<Button>("HaTest")!.IsEnabled = true;
            foreach (var name in new[] { "HaEnabled", "HaLocalTokenStorage", "HaUrl", "HaToken" })
                this.FindControl<Control>(name)!.IsEnabled = true;
            RefreshHaEntities();
        }
    }

    private bool _updatingHaSelection;

    private void RefreshHaActionMessage()
    {
        this.FindControl<TextBlock>("HaActionStatus")!.IsVisible = _haActionMessage is not null;
        if (_haActionMessage is not null) SetUiText("HaActionStatus", _haActionMessage);
    }

    private void ApplyHaAliases() => _pageSources.SetHomeAssistantAliases(
        _haPreferences.Select(preference => new KeyValuePair<string, string>(preference.EntityId, preference.Alias)));

    private string? SelectedHaEntityId() =>
        this.FindControl<ComboBox>("HaEntityPicker")!.SelectedItem as string;

    private string? SelectedHaAttribute() =>
        (this.FindControl<ListBox>("HaAttributes")!.SelectedItem as HaAttributeRow)?.Name;

    private string HaEntityKey(string id)
    {
        var alias = _haPreferences.FirstOrDefault(p => string.Equals(p.EntityId, id, StringComparison.OrdinalIgnoreCase))?.Alias;
        var normalized = PageSourceValues.NormalizeHaAlias(alias);
        return "{HA." + (normalized.Length > 0 ? normalized : id) + "}";
    }

    private string HaAttributeKey(string id, string name) =>
        HaEntityKey(id).TrimEnd('}') + ".attribute." + name + "}";

    private async Task CopyHaKeyAsync(string key)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null) await clipboard.SetTextAsync(key);
    }

    private void RefreshHaEntities()
    {
        if (_updatingHaSelection) return;
        var ha = _pageSources.HomeAssistant;
        var picker = this.FindControl<ComboBox>("HaEntityPicker")!;
        var previous = SelectedHaEntityId();
        var domains = new[] { UiText("All domains") }.Concat(ha.Entities.Select(e => e.Domain).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(d => d, StringComparer.OrdinalIgnoreCase)).ToArray();
        var domainPicker = this.FindControl<ComboBox>("HaDomainFilter")!;
        var selectedDomain = domainPicker.SelectedIndex > 0 ? domainPicker.SelectedItem as string : null;
        if (!domains.SequenceEqual(domainPicker.ItemsSource as string[] ?? Array.Empty<string>()))
        {
            _updatingHaSelection = true;
            domainPicker.ItemsSource = domains;
            domainPicker.SelectedItem = selectedDomain is not null && domains.Contains(selectedDomain) ? selectedDomain : domains[0];
            _updatingHaSelection = false;
        }
        var filter = this.FindControl<TextBox>("HaEntityFilter")!.Text?.Trim() ?? "";
        var domain = domainPicker.SelectedIndex > 0 ? domainPicker.SelectedItem as string : null;
        var entities = ha.Entities
            .Where(e => (domain is null || string.Equals(e.Domain, domain, StringComparison.OrdinalIgnoreCase))
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
        RefreshHaDetails(false);
        UiMessage version = ha.HomeAssistantVersion is null ? "unknown version" : (UiMessage)$"{ha.HomeAssistantVersion}";
        RefreshHaActionMessage();
        SetUiText("HaStatus", ha.IsConnected
            ? (UiMessage)$"Home Assistant connected ({version}), {ha.Entities.Count} entities, read-only."
            : ha.LastError is { Length: > 0 } error ? (UiMessage)$"Home Assistant disconnected: {error}"
            : ha.IsConfigured ? "Home Assistant connecting..." : "Home Assistant not configured");
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
        RefreshHaDetails(true);
    }

    private void RefreshHaDetails(bool loadPreferences)
    {
        var id = SelectedHaEntityId();
        var entity = _pageSources.HomeAssistant.Entities.FirstOrDefault(e => e.EntityId == id);
        var preference = _haPreferences.FirstOrDefault(p => string.Equals(p.EntityId, id, StringComparison.OrdinalIgnoreCase));
        SetUiText("HaSelectedEntity", entity is null
            ? "Select an entity above." : (UiMessage)$"{entity.FriendlyName} ({entity.EntityId}) — {entity.State} {entity.Unit}");
        if (loadPreferences)
        {
            this.FindControl<TextBox>("HaAlias")!.Text = preference?.Alias ?? "";
            this.FindControl<CheckBox>("HaFavorite")!.IsChecked = preference?.IsFavorite == true;
        }
        var attributes = entity?.Attributes.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new HaAttributeRow(pair.Key, Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture) ?? "")).ToArray() ?? [];
        var list = this.FindControl<ListBox>("HaAttributes")!;
        var previous = SelectedHaAttribute();
        if (!attributes.SequenceEqual(list.ItemsSource as HaAttributeRow[] ?? []))
        {
            list.ItemsSource = attributes;
            list.SelectedItem = attributes.FirstOrDefault(attribute => attribute.Name == previous) ?? attributes.FirstOrDefault();
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
