namespace LIS2.App.Avalonia;

/// <summary>Read-only dashboard presentation for the virtual display transport.</summary>
public static class DashboardConnectionState
{
    public static string Badge(bool connected) => connected ? "Connected (Virtual)" : "Disconnected";

    public static string Description(bool connected, int commandCount) =>
        connected
            ? $"Virtual LIS2 connected. Commands: {commandCount}."
            : "Virtual LIS2 is disconnected. Connect to preview display commands without hardware.";
}
