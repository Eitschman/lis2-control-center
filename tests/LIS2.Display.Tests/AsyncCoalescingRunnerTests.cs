using LIS2.Display;

namespace LIS2.Display.Tests;

public sealed class AsyncCoalescingRunnerTests
{
    [Fact]
    public async Task ConcurrentRequests_NeverOverlap_AndBurstIsCoalesced()
    {
        var runner = new AsyncCoalescingRunner();
        var entered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var executions = 0;
        var concurrent = 0;
        var maxConcurrent = 0;

        async Task Action()
        {
            var current = Interlocked.Increment(ref concurrent);
            maxConcurrent = Math.Max(maxConcurrent, current);
            var execution = Interlocked.Increment(ref executions);

            try
            {
                if (execution == 1)
                {
                    entered.TrySetResult();
                    await release.Task;
                }
            }
            finally
            {
                Interlocked.Decrement(ref concurrent);
            }
        }

        var first = runner.RunAsync(Action);
        await entered.Task;

        var burst = Enumerable.Range(0, 20)
            .Select(_ => runner.RunAsync(Action))
            .ToArray();

        await Task.WhenAll(burst);
        release.TrySetResult();
        await first;

        Assert.Equal(1, maxConcurrent);
        Assert.Equal(2, executions);
    }

    [Fact]
    public async Task FailedExecution_DoesNotPoisonFutureRequests()
    {
        var runner = new AsyncCoalescingRunner();
        var first = true;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => runner.RunAsync(() =>
            {
                if (first)
                {
                    first = false;
                    throw new InvalidOperationException("boom");
                }

                return Task.CompletedTask;
            }));

        await runner.RunAsync(() => Task.CompletedTask);
    }
}
