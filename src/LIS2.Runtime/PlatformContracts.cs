namespace LIS2.Runtime;

/// <summary>Platform capability contracts; provider implementations belong to their respective OS adapters.</summary>
public interface IPlatformSerialPortDiscovery
{
    IReadOnlyList<string> GetAvailablePorts();
}

public interface IPlatformSensorProvider
{
    IReadOnlyDictionary<string, double?> Snapshot();
}

public interface IPlatformMediaProvider
{
    IReadOnlyDictionary<string, object?> Snapshot();
}

public interface IPlatformTrayService
{
    Task ShowAsync(CancellationToken cancellationToken = default);
    Task HideAsync(CancellationToken cancellationToken = default);
}

public interface IPlatformAutostartService
{
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default);
    Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
}

public interface IPlatformSecretStore
{
    Task<string?> ReadAsync(string key, CancellationToken cancellationToken = default);
    Task WriteAsync(string key, string? value, CancellationToken cancellationToken = default);
}

public interface IPlatformAppearance
{
    bool IsDarkThemePreferred { get; }
    event EventHandler? Changed;
}
