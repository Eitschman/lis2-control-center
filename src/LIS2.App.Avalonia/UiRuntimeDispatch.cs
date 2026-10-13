using Avalonia.Threading;

namespace LIS2.App.Avalonia;

/// <summary>
/// Awaitable UI dispatch bridge for headless periodic loops. Unlike Post, the
/// caller does not report a tick completed until its UI action has completed.
/// </summary>
internal static class UiRuntimeDispatch
{
    public static Task RunAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(async () =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                completion.TrySetCanceled(cancellationToken);
                return;
            }
            try
            {
                await action();
                completion.TrySetResult(true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                completion.TrySetCanceled(cancellationToken);
            }
            catch (Exception ex) { completion.TrySetException(ex); }
        });
        return completion.Task;
    }
}
