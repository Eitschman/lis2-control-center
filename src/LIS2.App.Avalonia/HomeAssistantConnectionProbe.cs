using LIS2.Sources;

namespace LIS2.App.Avalonia;

/// <summary>Getrennte Anmeldung ohne Tokenpersistenz, Quellumschaltung oder Ausgabe fremder Servermeldungen.</summary>
internal sealed class HomeAssistantConnectionProbe(string? address, string? token)
{
    private readonly HashSet<HomeAssistantConnectionStage> _completed = [];
    private string _connectionError = "WebSocket connection failed";
    private bool _timedOut;

    internal IReadOnlyList<ConnectionTestStep> Steps() =>
    [
        ConnectionTest.Check("Home Assistant address", Uri.TryCreate(address, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https",
            "A valid HTTP or HTTPS address is required", "Check the Home Assistant address in Home Assistant settings"),
        ConnectionTest.Check("Access token", !string.IsNullOrWhiteSpace(token),
            "An access token is required", "Enter a long-lived Home Assistant access token"),
        new("WebSocket connection", "Check the server, network, certificate and reverse proxy", async cancellation =>
        {
            _completed.Clear(); _timedOut = false; _connectionError = "WebSocket connection failed";
            await using var probe = new HomeAssistantDataSource();
            probe.Configure(address, token);
            try { await probe.TestConnectionAsync(cancellation, stage => _completed.Add(stage)); }
            catch (OperationCanceledException) when (!cancellation.IsCancellationRequested) { _timedOut = true; _connectionError = "Connection test timed out"; }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception) { _connectionError = ConnectionTest.SafeError(exception); }
            return _completed.Contains(HomeAssistantConnectionStage.WebSocketConnected) ? null : _connectionError;
        }),
        new("WebSocket greeting", "Check that the address points to a Home Assistant server", _ =>
            Task.FromResult(_completed.Contains(HomeAssistantConnectionStage.GreetingAccepted) ? null : _timedOut ? "Connection test timed out" : "Unexpected Home Assistant greeting")),
        new("Home Assistant authentication", "Check token validity and the Home Assistant user account", _ =>
            Task.FromResult(_completed.Contains(HomeAssistantConnectionStage.Authenticated) ? null : _timedOut ? "Connection test timed out" : "Home Assistant authentication was rejected"))
    ];
}
