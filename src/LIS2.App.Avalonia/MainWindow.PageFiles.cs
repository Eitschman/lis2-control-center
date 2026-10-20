using Avalonia.Controls;
using Avalonia.Platform.Storage;
using LIS2.Display;
using System.IO;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private void InitializePageTools()
    {
        this.FindControl<ComboBox>("PageConditionOperator")!.SelectedIndex = 0;
        this.FindControl<Button>("PageApplyCondition")!.Click += (_, _) => ApplyPageCondition();
        this.FindControl<TextBox>("PageVisibility")!.TextChanged += (_, _) => UpdateConditionStatus();
        this.FindControl<TextBox>("PageConditionKey")!.TextChanged += (_, _) => UpdateConditionStatus();
        this.FindControl<TextBox>("PageConditionValue")!.TextChanged += (_, _) => UpdateConditionStatus();
        this.FindControl<ComboBox>("PageConditionOperator")!.SelectionChanged += (_, _) => UpdateConditionStatus();
        this.FindControl<Button>("PageImport")!.Click += async (_, _) => await RunPageFileActionAsync(LoadPagesAsync);
        this.FindControl<Button>("PageExport")!.Click += async (_, _) => await RunPageFileActionAsync(SavePagesAsync);
    }

    private void ApplyPageCondition()
    {
        var key = this.FindControl<TextBox>("PageConditionKey")!.Text ?? "";
        var op = (this.FindControl<ComboBox>("PageConditionOperator")!.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "=";
        var value = this.FindControl<TextBox>("PageConditionValue")!.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            SetVirtualStatus("Condition requires a key and comparison value.");
            return;
        }
        this.FindControl<TextBox>("PageVisibility")!.Text = $"{key.Trim()}{op}{value}";
        UpdateConditionStatus();
    }

    private void UpdateConditionStatus()
    {
        var expression = this.FindControl<TextBox>("PageVisibility")!.Text;
        var valid = string.IsNullOrWhiteSpace(expression) ||
                    VisibilityEvaluator.TryParse(expression, out _, out _, out _);
        var status = this.FindControl<TextBlock>("PageConditionStatus")!;
        if (!valid)
        {
            status.Text = BrowserText("Invalid condition syntax.");
            return;
        }
        if (string.IsNullOrWhiteSpace(expression))
        {
            status.Text = BrowserText("No visibility condition (always visible).");
            return;
        }
        try
        {
            var visible = new VisibilityEvaluator().IsVisible(expression, PagePreviewValues());
            status.Text = visible
                ? BrowserText("Condition currently matches.")
                : BrowserText("Condition currently does not match.");
        }
        catch (Exception)
        {
            status.Text = BrowserText("Condition cannot be evaluated with current values.");
        }
    }

    private async Task RunPageFileActionAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { SetVirtualStatus($"Page file error: {ex.Message}"); }
    }

    private async Task SavePagesAsync()
    {
        // Export must reflect the currently edited page, not its previous
        // persisted values. Invalid drafts stay in the editor for correction.
        if (!CommitPendingPageEditor()) return;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save page definitions",
            SuggestedFileName = "lis2-pages.json"
        });
        if (file is null) return;
        await using var stream = await file.OpenWriteAsync();
        stream.SetLength(0);
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(_pages.ExportJson());
        await writer.FlushAsync();
        SetVirtualStatus("Page definitions exported.");
    }

    private async Task LoadPagesAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load page definitions",
            AllowMultiple = false
        });
        if (files.Count == 0) return;
        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream);
        var imported = await reader.ReadToEndAsync();
        // Validate the file before touching either the workspace or its editor.
        // PageWorkspace.ImportJson is transactional on invalid input.
        var candidate = new PageWorkspace();
        candidate.ImportJson(imported);
        if (!CommitPendingPageEditor()) return;
        _pages.ImportJson(imported);
        RefreshPageList(_pages.Pages.Count > 0 ? 0 : -1);
        await ReloadRunningPagesAsync();
        await SavePortableWorkspaceAsync();
        SetVirtualStatus("Page definitions imported.");
    }
}
