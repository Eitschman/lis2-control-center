namespace LIS2.Core;

public sealed class VirtualLis2Transport : ILis2Transport
{
    private readonly List<byte[]> _writes = new();

    public bool IsOpen { get; private set; }

    public IReadOnlyList<byte[]> Writes => _writes;

    public VirtualLis2State State { get; } = new();

    public event EventHandler<VirtualLis2WriteEventArgs>? Written;
    public event EventHandler? StateChanged;

    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsOpen = true;
        return Task.CompletedTask;
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsOpen = false;
        return Task.CompletedTask;
    }

    public Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsOpen)
            throw new InvalidOperationException("Virtual LIS2 transport is not connected.");

        var copy = data.ToArray();
        _writes.Add(copy);

        VirtualLis2ProtocolInterpreter.Apply(State, copy);

        Written?.Invoke(this, new VirtualLis2WriteEventArgs(copy));
        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsOpen = false;
        return ValueTask.CompletedTask;
    }
}

public sealed class VirtualLis2WriteEventArgs : EventArgs
{
    public VirtualLis2WriteEventArgs(byte[] data) => Data = data;

    public byte[] Data { get; }
}
