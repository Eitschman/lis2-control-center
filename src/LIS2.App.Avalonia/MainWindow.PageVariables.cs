using Avalonia.Controls;
using Avalonia.Layout;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private sealed record VariableRow(string Key, string Label)
    {
        public override string ToString() => Label + "  [" + Key + "]";
    }
    private string? _chosenPageVariable;
    private readonly HashSet<string> _favoritePageVariables = new(StringComparer.OrdinalIgnoreCase);

    private void InitializePageVariables()
    {
        this.FindControl<Button>("PageRefreshVariables")!.Click += (_, _) => RefreshPageVariables();
        this.FindControl<Button>("PageInsertLine1")!.Click += (_, _) => InsertPageVariable("PageLine1");
        this.FindControl<Button>("PageInsertLine2")!.Click += (_, _) => InsertPageVariable("PageLine2");
        this.FindControl<Button>("PageVariableBrowser")!.Click += async (_, _) => await ShowPageVariableBrowserAsync();
        RefreshPageVariables();
    }

    private void RefreshPageVariables()
    {
        var count = PagePreviewValues().Count;
        this.FindControl<TextBlock>("PageVariableStatus")!.Text =
            $"{count} variables available. Browse to search by category and name.";
        RefreshPagePreview();
    }

    private void InsertPageVariable(string target)
    {
        if (string.IsNullOrWhiteSpace(_chosenPageVariable))
        {
            _ = ShowPageVariableBrowserAsync(target);
            return;
        }
        InsertSelectedPageVariable(target, _chosenPageVariable);
    }

    private void InsertSelectedPageVariable(string target, string name)
    {
        var input = this.FindControl<TextBox>(target)!;
        var old = input.Text ?? string.Empty;
        var position = Math.Clamp(input.CaretIndex, 0, old.Length);
        var token = "{" + name + "}";
        input.Text = old.Insert(position, token);
        input.CaretIndex = position + token.Length;
        input.Focus();
        RefreshPagePreview();
    }

    private async Task ShowPageVariableBrowserAsync(string? insertTarget = null)
    {
        // Snapshot is taken on opening, not on every two-second sensor refresh.
        var snapshot = PagePreviewValues();
        var keys = snapshot.Keys.ToArray();
        var dialog = new Window
        {
            Title = "Browse template variables", Width = 850, Height = 630,
            MinWidth = 580, MinHeight = 420, WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var search = new TextBox { Watermark = "Search variable keys (multiple words)…" };
        var category = new ComboBox
        {
            ItemsSource = new[] { "All", "Favorites", "Home Assistant", "Hardware", "Clock", "Media / Other" },
            SelectedIndex = 0, MinWidth = 150
        };
        var entityPicker = new ComboBox { MinWidth = 160 };
        var results = new ListBox { MinHeight = 240 };
        var count = new TextBlock();
        var details = new TextBlock { TextWrapping = global::Avalonia.Media.TextWrapping.Wrap };
        var favorite = new Button { Content = "☆ Add favorite" };
        var insert1 = new Button { Content = "Insert into line 1" };
        var insert2 = new Button { Content = "Insert into line 2" };
        var close = new Button { Content = "Close" };
        var refresh = new Button { Content = "Refresh values" };
        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto,Auto,Auto"), RowSpacing = 10, Margin = new global::Avalonia.Thickness(16) };
        var filters = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        filters.Children.Add(search);
        filters.Children.Add(category);
        filters.Children.Add(entityPicker);
        Grid.SetRow(filters, 0); layout.Children.Add(filters);
        Grid.SetRow(count, 1); layout.Children.Add(count);
        Grid.SetRow(results, 2); layout.Children.Add(results);
        Grid.SetRow(details, 3); layout.Children.Add(details);
        Grid.SetRow(favorite, 4); layout.Children.Add(favorite);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Children.Add(insert1); actions.Children.Add(insert2); actions.Children.Add(refresh); actions.Children.Add(close);
        Grid.SetRow(actions, 5); layout.Children.Add(actions);
        dialog.Content = layout;

        var friendlyNames = _pageSources.HomeAssistant.Entities.ToDictionary(e => e.EntityId, e => e.FriendlyName, StringComparer.OrdinalIgnoreCase);
        string? Selected() => (results.SelectedItem as VariableRow)?.Key;
        void RefreshEntities()
        {
            var old = entityPicker.SelectedItem as string;
            var entities = new[] { "All entities" }.Concat(keys.Where(key => PageVariableBrowserCatalog.Category(key) == "Home Assistant")
                .Select(PageVariableBrowserCatalog.Entity).Where(entity => entity.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(entity => entity, StringComparer.OrdinalIgnoreCase)).ToArray();
            entityPicker.ItemsSource = entities;
            entityPicker.SelectedItem = old is not null && entities.Contains(old) ? old : "All entities";
        }
        void UpdateDetails()
        {
            var key = Selected();
            if (key is null) { details.Text = "Select a variable to inspect its current value and exact template key."; return; }
            snapshot.TryGetValue(key, out var value);
            details.Text = key + Environment.NewLine + "Current value: " + (value?.ToString() ?? "(unavailable)") +
                Environment.NewLine + "Template: {" + key + "}";
            favorite.Content = _favoritePageVariables.Contains(key) ? "★ Remove favorite" : "☆ Add favorite";
            _chosenPageVariable = key;
        }
        void Filter()
        {
            var selected = Selected();
            var terms = (search.Text ?? "").Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var matching = keys.Where(key => terms.All(term =>
                key.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                PageVariableBrowserCatalog.Label(key, friendlyNames).Contains(term, StringComparison.OrdinalIgnoreCase)));
            var entity = entityPicker.SelectedItem as string;
            if (!string.IsNullOrEmpty(entity) && entity != "All entities")
                matching = matching.Where(key => string.Equals(PageVariableBrowserCatalog.Entity(key), entity, StringComparison.OrdinalIgnoreCase));
            var filtered = PageVariableBrowserCatalog.Search(matching, null, category.SelectedItem as string,
                _favoritePageVariables);
            var rows = filtered.Select(key => new VariableRow(key, PageVariableBrowserCatalog.Label(key, friendlyNames))).ToArray();
            results.ItemsSource = rows;
            results.SelectedItem = rows.FirstOrDefault(row => string.Equals(row.Key, selected, StringComparison.OrdinalIgnoreCase)) ?? rows.FirstOrDefault();
            count.Text = $"{rows.Length} shown (maximum 50) of {keys.Length} available variables";
            UpdateDetails();
        }
        search.TextChanged += (_, _) => Filter();
        category.SelectionChanged += (_, _) => Filter();
        entityPicker.SelectionChanged += (_, _) => Filter();
        results.SelectionChanged += (_, _) => UpdateDetails();
        favorite.Click += (_, _) =>
        {
            if (Selected() is not { } key) return;
            if (!_favoritePageVariables.Add(key)) _favoritePageVariables.Remove(key);
            Filter();
            _ = SavePortableWorkspaceAsync();
        };
        insert1.Click += (_, _) => { if (Selected() is { } key) { InsertSelectedPageVariable("PageLine1", key); dialog.Close(); } };
        insert2.Click += (_, _) => { if (Selected() is { } key) { InsertSelectedPageVariable("PageLine2", key); dialog.Close(); } };
        refresh.Click += (_, _) =>
        {
            snapshot = PagePreviewValues();
            keys = snapshot.Keys.ToArray();
            friendlyNames = _pageSources.HomeAssistant.Entities.ToDictionary(e => e.EntityId, e => e.FriendlyName, StringComparer.OrdinalIgnoreCase);
            RefreshEntities();
            Filter();
        };
        close.Click += (_, _) => dialog.Close();
        RefreshEntities();
        Filter();
        if (insertTarget is "PageLine1" or "PageLine2")
            details.Text = "Select a variable, then insert it into the requested line.";
        await dialog.ShowDialog(this);
    }
}
