using Avalonia.Controls;
using Avalonia.Platform.Storage;
using LIS2.Display;
using System.IO;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private void InitializePageTools()
    {
        var keys = this.FindControl<ComboBox>("PageConditionKey")!;
        keys.ItemsSource = new[] { "Winamp.State", "Winamp.Title", "HA.sensor.temperature", "Cpu.Load", "Clock.Time" };
        this.FindControl<ComboBox>("PageConditionOperator")!.SelectedIndex = 0;
        this.FindControl<Button>("PageApplyCondition")!.Click += (_, _) => ApplyPageCondition();
        this.FindControl<TextBox>("PageVisibility")!.TextChanged += (_, _) => UpdateConditionStatus();
        this.FindControl<Button>("PageImport")!.Click += async (_, _) => await RunPageFileActionAsync(LoadPagesAsync);
        this.FindControl<Button>("PageExport")!.Click += async (_, _) => await RunPageFileActionAsync(SavePagesAsync);
    }

    private void ApplyPageCondition()
    {
        var keyBox = this.FindControl<ComboBox>("PageConditionKey")!;
        var key = keyBox.SelectedItem?.ToString() ?? keyBox.Text ?? "";
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
        this.FindControl<TextBlock>("PageConditionStatus")!.Text = valid
            ? "Condition syntax valid (runtime values not connected yet)."
            : "Invalid condition syntax.";
    }

    private async Task RunPageFileActionAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { SetVirtualStatus($"Page file error: {ex.Message}"); }
    }

    private async Task SavePagesAsync()
    {
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
        _pages.ImportJson(await reader.ReadToEndAsync());
        RefreshPageList(_pages.Pages.Count > 0 ? 0 : -1);
        SetVirtualStatus("Page definitions imported.");
    }
}
