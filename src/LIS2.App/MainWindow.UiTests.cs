using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace LIS2.App;

public partial class MainWindow
{
    private async Task RunUiRegressionTestAsync()
    {
        var outputRoot = ResolveUiTestOutputPath();
        var screenshotsRoot = Path.Combine(outputRoot, "screenshots");
        var reportPath = Path.Combine(outputRoot, "ui-regression-report.txt");
        var report = new UiRegressionReport();

        Directory.CreateDirectory(screenshotsRoot);

        _pageTimer.Stop();
        _fanTimer.Stop();
        _hardwareTimer.Stop();
        _winampTimer.Stop();
        _eventUiTimer.Stop();
        _pageEditorPreviewTimer.Stop();

        var originalTheme = ThemeService.Mode;
        var originalLanguage = LocalizationService.Mode;
        var originalWidth = Width;
        var originalHeight = Height;

        try
        {
            HelpLocalization.ValidateCoverage();

            WindowState = WindowState.Normal;
            Width = 1320;
            Height = 900;
            Show();
            Activate();
            UpdateLayout();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

            var themes = new[]
            {
                AppThemeMode.Light,
                AppThemeMode.Dark
            };

            var languages = new[]
            {
                AppLanguageMode.English,
                AppLanguageMode.German
            };

            foreach (var theme in themes)
            {
                ThemeService.Apply(theme);
                ThemeService.ApplyWindowChrome(this);

                foreach (var language in languages)
                {
                    LocalizationService.Apply(language);
                    LocalizationService.ApplyTo(this);
                    RefreshLocalizedSectionHeader();
                    UpdateTransportUi();
                    UpdateConnectionUiLocalization();

                    for (var index = 0; index < MainTabs.Items.Count; index++)
                    {
                        MainTabs.SelectedIndex = index;
                        UpdateNavigationSelection(index);
                        RefreshLocalizedSectionHeader();
                        UpdateLayout();
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

                        ValidateMainWindowLayout(report, index);

                        var prefix =
                            $"{theme.ToString().ToLowerInvariant()}-" +
                            $"{language.ToString().ToLowerInvariant()}-" +
                            $"{index:00}-{UiTestSupport.Slug(Sections[index].Title)}";

                        var mainPath = Path.Combine(
                            screenshotsRoot,
                            $"{prefix}-main.png");
                        UiTestSupport.CapturePng(this, mainPath);
                        report.Info($"Captured {Path.GetFileName(mainPath)}");

                        var help = new HelpWindow(
                            index,
                            CreateDisplayValues(),
                            _settings.CustomGlyphs.Select(glyph => glyph.Name).ToArray())
                        {
                            Owner = this,
                            Width = 1080,
                            Height = 780
                        };

                        help.Show();
                        help.UpdateLayout();
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

                        help.ValidateLayoutForUiTest(report);

                        var helpPath = Path.Combine(
                            screenshotsRoot,
                            $"{prefix}-help.png");
                        UiTestSupport.CapturePng(help, helpPath);
                        report.Info($"Captured {Path.GetFileName(helpPath)}");

                        help.Close();
                    }
                }
            }

            report.Info($"Completed UI regression run with {report.FailureCount} failure(s).");
            report.Write(reportPath);

            if (report.FailureCount > 0)
            {
                throw new InvalidOperationException(
                    $"UI regression checks failed: {report.FailureCount}. " +
                    $"See {reportPath} and screenshots.");
            }

            Log("INFO GitHub UI regression test completed");
            Environment.ExitCode = 0;
        }
        catch (Exception ex)
        {
            report.Fail($"Unhandled UI regression exception: {ex}");
            report.Write(reportPath);
            Log($"ERR  GitHub UI regression test: {ex}");
            Environment.ExitCode = 1;
        }
        finally
        {
            LocalizationService.Apply(originalLanguage);
            LocalizationService.ApplyTo(this);
            ThemeService.Apply(originalTheme);
            Width = originalWidth;
            Height = originalHeight;

            _allowClose = true;
            Close();
            System.Windows.Application.Current.Shutdown(Environment.ExitCode);
        }
    }

    private void ValidateMainWindowLayout(UiRegressionReport report, int sectionIndex)
    {
        var sectionName = Sections[sectionIndex].Title;

        UiTestSupport.AssertPositiveSize(
            report,
            MainTabs,
            $"{sectionName}: main tabs");

        UiTestSupport.AssertFullyInside(
            report,
            SectionHelpButton,
            SectionHeaderGrid,
            $"{sectionName}: help button");

        UiTestSupport.AssertFullyInside(
            report,
            ConnectionBadge,
            SectionHeaderGrid,
            $"{sectionName}: connection badge");

        if (sectionIndex != 9)
            return;

        UiTestSupport.AssertPositiveSize(
            report,
            HomeAssistantDetailBorder,
            "Home Assistant: detail area");

        UiTestSupport.AssertFullyInside(
            report,
            HomeAssistantDetailBorder,
            MainTabs,
            "Home Assistant: detail area");

        UiTestSupport.AssertFullyInside(
            report,
            HomeAssistantEntityActionsPanel,
            HomeAssistantDetailBorder,
            "Home Assistant: entity actions");

        UiTestSupport.AssertFullyInside(
            report,
            HomeAssistantAttributeActionsPanel,
            HomeAssistantDetailBorder,
            "Home Assistant: attribute actions");
    }

    private static string ResolveUiTestOutputPath()
    {
        var args = Environment.GetCommandLineArgs();

        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(
                    args[index],
                    "--ui-test-output",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(args[index + 1]);
            }
        }

        return Path.GetFullPath(
            Path.Combine(
                Environment.CurrentDirectory,
                "artifacts",
                "ui-tests"));
    }

    private static bool IsUiTestMode() =>
        Environment.GetCommandLineArgs().Any(
            arg => string.Equals(
                arg,
                "--ui-test",
                StringComparison.OrdinalIgnoreCase));
}
