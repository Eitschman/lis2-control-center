using System.ComponentModel;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace LIS2.App.Avalonia;

internal enum ConnectionTestState { Pending, Running, Passed, Failed, Skipped, Cancelled }
internal sealed record ConnectionTestResult(string Name, ConnectionTestState State, string Detail = "", string Remedy = "");
internal sealed record ConnectionTestStep(string Name, string Remedy, Func<CancellationToken, Task<string?>> Check,
    bool IsApplicable = true);

/// <summary>Schritte abbrechbar ausführen; nach Fehlern keine abhängigen Prüfungen als erfolgreich ausgeben.</summary>
internal static class ConnectionTest
{
    internal static async Task<IReadOnlyList<ConnectionTestResult>> RunAsync(IReadOnlyList<ConnectionTestStep> steps,
        Action<IReadOnlyList<ConnectionTestResult>> progress, CancellationToken cancellationToken,
        TimeSpan? timeout = null)
    {
        var results = steps.Select(step => new ConnectionTestResult(step.Name, ConnectionTestState.Pending)).ToArray();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout ?? TimeSpan.FromSeconds(12));
        for (var index = 0; index < steps.Count; index++)
        {
            if (!steps[index].IsApplicable)
            {
                results[index] = results[index] with { State = ConnectionTestState.Skipped, Detail = "Start playback to test audio" };
                progress(results.ToArray());
                continue;
            }
            results[index] = results[index] with { State = ConnectionTestState.Running };
            progress(results.ToArray());
            try
            {
                deadline.Token.ThrowIfCancellationRequested();
                // Den kooperativen Abbruch einschließlich Socket-/Prozessbereinigung abwarten.
                var failure = await steps[index].Check(deadline.Token);
                results[index] = results[index] with
                {
                    State = failure is null ? ConnectionTestState.Passed : ConnectionTestState.Failed,
                    Detail = failure ?? "", Remedy = failure is null ? "" : steps[index].Remedy
                };
            }
            catch (OperationCanceledException)
            {
                results[index] = results[index] with
                {
                    State = cancellationToken.IsCancellationRequested ? ConnectionTestState.Cancelled : ConnectionTestState.Failed,
                    Detail = cancellationToken.IsCancellationRequested ? "Test cancelled" : "Connection test timed out",
                    Remedy = cancellationToken.IsCancellationRequested ? "" : steps[index].Remedy
                };
            }
            catch (Exception exception)
            {
                results[index] = results[index] with { State = ConnectionTestState.Failed,
                    Detail = SafeError(exception), Remedy = steps[index].Remedy };
            }
            if (results[index].State != ConnectionTestState.Passed)
            {
                for (var remaining = index + 1; remaining < results.Length; remaining++)
                    results[remaining] = results[remaining] with { State = ConnectionTestState.Skipped };
                progress(results.ToArray());
                return results;
            }
            progress(results.ToArray());
        }
        return results;
    }

    /// <summary>Keine Servertexte, URLs, Argumente oder Ausnahme-Nachrichten mit Zugangsdaten übernehmen.</summary>
    internal static string SafeError(Exception exception) => exception switch
    {
        UnauthorizedAccessException => "Access denied",
        SocketException socket => "Network error" + " (" + socket.SocketErrorCode + ")",
        WebSocketException webSocket when webSocket.InnerException is { } inner => SafeError(inner),
        WebSocketException => "WebSocket connection failed",
        HttpRequestException request when request.InnerException is { } inner => SafeError(inner),
        HttpRequestException => "HTTP connection failed",
        Win32Exception win32 => "Operating system error" + " (" + win32.NativeErrorCode + ")",
        FileNotFoundException => "Required component is missing",
        IOException => "Input/output error",
        _ => "Connection check failed"
    };

    internal static ConnectionTestStep Check(string name, bool passed, string failure, string remedy) =>
        new(name, remedy, token => { token.ThrowIfCancellationRequested(); return Task.FromResult(passed ? null : failure); });
}
