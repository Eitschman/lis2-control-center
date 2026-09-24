using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LIS2.App;

public static class AppInfo
{
    public const string RepositoryUrl = "https://github.com/Eitschman/lis2-control-center";

    public static string ProductName => "LIS2 Control Center";

    public static string Version
    {
        get
        {
            var assembly = typeof(AppInfo).Assembly;
            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informational))
                return informational.Split('+')[0];

            return assembly.GetName().Version?.ToString(3) ?? "unknown";
        }
    }

    public static string BuildConfiguration =>
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    public static string Runtime =>
        RuntimeInformation.FrameworkDescription;

    public static string Architecture =>
        RuntimeInformation.ProcessArchitecture.ToString();

    public static string OperatingSystem =>
        RuntimeInformation.OSDescription;

    public static string DataDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LIS2ControlCenter");
}
