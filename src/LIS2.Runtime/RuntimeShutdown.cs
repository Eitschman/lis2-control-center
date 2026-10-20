namespace LIS2.Runtime;

/// <summary>Führt sämtliche Aufräumschritte aus und meldet Fehler erst nach dem letzten Schritt.</summary>
public static class RuntimeShutdown
{
    public static async Task RunAsync(params Func<Task>[] steps)
    {
        List<Exception>? errors = null;
        foreach (var step in steps)
        {
            try { await step(); }
            catch (Exception exception) { (errors ??= new()).Add(exception); }
        }
        if (errors is not null) throw new AggregateException("Runtime shutdown failed.", errors);
    }
}
