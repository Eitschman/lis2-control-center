using LIS2.Sources;

namespace LIS2.Sources.Tests;

public sealed class DataSourceRegistryTests
{
    [Fact]
    public void Snapshot_PrefixesValuesWithSourceId()
    {
        var registry = new DataSourceRegistry();
        registry.Add(new FakeSource("CPU", new Dictionary<string, object?>
        {
            ["Load"] = 42
        }));

        var snapshot = registry.Snapshot();

        Assert.Equal(42, snapshot["CPU.Load"]);
    }

    [Fact]
    public void Add_RejectsDuplicateIds()
    {
        var registry = new DataSourceRegistry();
        registry.Add(new FakeSource("CPU", new Dictionary<string, object?>()));

        Assert.Throws<InvalidOperationException>(() =>
            registry.Add(new FakeSource("CPU", new Dictionary<string, object?>())));
    }

    private sealed class FakeSource : IDataSource
    {
        public FakeSource(string id, IReadOnlyDictionary<string, object?> values)
        {
            Id = id;
            Values = values;
        }

        public string Id { get; }

        public IReadOnlyDictionary<string, object?> Values { get; }

        public event EventHandler? Changed
        {
            add { }
            remove { }
        }

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task StartAll_IsolatesFailingSource()
    {
        var registry = new DataSourceRegistry();
        registry.Add(new ThrowingSource("Broken"));
        registry.Add(new FakeSource("Good", new Dictionary<string, object?>
        {
            ["Value"] = 1
        }));

        await registry.StartAllAsync();

        Assert.NotNull(registry.Errors["Broken"]);
        Assert.Null(registry.Errors["Good"]);
        Assert.Equal(1, registry.Snapshot()["Good.Value"]);
    }

    private sealed class ThrowingSource : IDataSource
    {
        public ThrowingSource(string id) => Id = id;

        public string Id { get; }

        public IReadOnlyDictionary<string, object?> Values { get; } =
            new Dictionary<string, object?>();

        public event EventHandler? Changed
        {
            add { }
            remove { }
        }

        public Task StartAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("boom");

        public Task StopAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
    [Fact]
    public async Task Snapshot_RemainsStableWhileSourcePublishesNewSnapshots()
    {
        var registry = new DataSourceRegistry();
        var source = new SwappingSource("Live");
        registry.Add(source);

        var writer = Task.Run(() =>
        {
            for (var index = 0; index < 500; index++)
                source.Publish(index);
        });

        for (var index = 0; index < 500; index++)
        {
            var snapshot = registry.Snapshot();
            Assert.True(snapshot.ContainsKey("Live.Value"));
        }

        await writer;
    }

    private sealed class SwappingSource : IDataSource
    {
        private IReadOnlyDictionary<string, object?> _values =
            new Dictionary<string, object?> { ["Value"] = 0 };

        public SwappingSource(string id) => Id = id;

        public string Id { get; }

        public IReadOnlyDictionary<string, object?> Values =>
            Volatile.Read(ref _values);

        public event EventHandler? Changed;

        public void Publish(int value)
        {
            Volatile.Write(
                ref _values,
                new Dictionary<string, object?> { ["Value"] = value });
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public Task StartAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
