using Avalonia;
using Avalonia.Headless;
using LIS2.App.Avalonia;

[assembly: AvaloniaTestApplication(typeof(LIS2.App.Avalonia.Tests.UiTestApp))]

namespace LIS2.App.Avalonia.Tests;

public static class UiTestApp
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia().WithInterFont()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
