using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace LIS2.App.Avalonia;

/// <summary>
/// Per-user HA credentials: DPAPI on Windows, freedesktop Secret Service
/// (libsecret's secret-tool) on Linux. Legacy local AES files are migrated
/// only after successful writing and verification in the native store.
/// </summary>
public sealed class PortableHaTokenStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("LIS2ControlCenter.HomeAssistant.AccessToken.v1");
    private readonly string _directory;
    private readonly LegacyHaTokenStore _legacy;
    private readonly string _secretTool;
    private readonly Func<string, string?, string?>? _nativeOperation;
    private readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Lokale AES-Dateien nur nach ausdrücklicher Auswahl verwenden.</summary>
    public bool UseLocalStorage { get; set; }

    public PortableHaTokenStore(string? directory = null, string secretTool = "secret-tool")
    {
        _directory = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LIS2ControlCenter");
        _legacy = new LegacyHaTokenStore(_directory);
        _secretTool = secretTool;
    }

    internal PortableHaTokenStore(string directory, Func<string, string?, string?> nativeOperation)
        : this(directory) => _nativeOperation = nativeOperation;

    internal PortableHaTokenStore(string directory, string secretTool, TimeSpan requestTimeout)
        : this(directory, secretTool) => _requestTimeout = requestTimeout;

    public void Save(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        if (UseLocalStorage)
        {
            _legacy.Save(token);
            if (_legacy.Load() != token) throw new CryptographicException("Local credential store verification failed.");
            return;
        }
        if (_nativeOperation is not null) _nativeOperation("store", token);
        else if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(_directory);
            var bytes = Encoding.UTF8.GetBytes(token);
            try
            {
                var protectedBytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
                var temp = Path.Combine(_directory, "ha-token.dpapi.tmp");
                File.WriteAllBytes(temp, protectedBytes);
                File.Move(temp, Path.Combine(_directory, "ha-token.dpapi"), true);
                CryptographicOperations.ZeroMemory(protectedBytes);
            }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }
        else if (OperatingSystem.IsLinux())
        {
            RunSecretTool("store", token);
        }
        else throw new PlatformNotSupportedException("No supported native credential store on this platform.");

        // Only remove old encrypted data once the native store can return the new token.
        if (LoadNative() == token)
        {
            _legacy.Delete();
            var key = Path.Combine(_directory, "ha-token.key");
            if (File.Exists(key)) File.Delete(key);
        }
        else throw new CryptographicException("Native credential store verification failed.");
    }

    public string? Load()
    {
        if (UseLocalStorage) return _legacy.Load();
        var native = LoadNative();
        if (!string.IsNullOrEmpty(native)) return native;
        var legacy = _legacy.Load();
        if (string.IsNullOrEmpty(legacy)) return null;
        // Bei gesperrtem Keyring bleiben die alten Dateien erhalten; der Fehler
        // wird angezeigt, statt automatisch die lokale Speicherung zu verwenden.
        Save(legacy);
        return legacy;
    }

    public void Delete()
    {
        if (UseLocalStorage) { DeleteLocal(); return; }
        if (_nativeOperation is not null) _nativeOperation("clear", null);
        else if (OperatingSystem.IsWindows())
        {
            var path = Path.Combine(_directory, "ha-token.dpapi");
            if (File.Exists(path)) File.Delete(path);
        }
        else if (OperatingSystem.IsLinux())
        {
            RunSecretTool("clear", null);
        }
        DeleteLocal();
    }

    private void DeleteLocal()
    {
        _legacy.Delete();
        var key = Path.Combine(_directory, "ha-token.key");
        if (File.Exists(key)) File.Delete(key);
    }

    private string? LoadNative()
    {
        if (_nativeOperation is not null) return _nativeOperation("lookup", null);
        if (OperatingSystem.IsWindows())
        {
            var path = Path.Combine(_directory, "ha-token.dpapi");
            if (!File.Exists(path)) return null;
            var bytes = ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.CurrentUser);
            try { return Encoding.UTF8.GetString(bytes); }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }
        if (OperatingSystem.IsLinux()) return RunSecretTool("lookup", null);
        throw new PlatformNotSupportedException();
    }

    private string? RunSecretTool(string operation, string? token) =>
        RunSecretToolAsync(operation, token).GetAwaiter().GetResult();

    private async Task<string?> RunSecretToolAsync(string operation, string? token)
    {
        var psi = new ProcessStartInfo(_secretTool)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add(operation);
        if (operation == "store")
        {
            psi.ArgumentList.Add("--label=LIS2 Control Center Home Assistant");
        }
        psi.ArgumentList.Add("application");
        psi.ArgumentList.Add("lis2-control-center");
        psi.ArgumentList.Add("credential");
        psi.ArgumentList.Add("home-assistant-token");
        using var process = new Process { StartInfo = psi };
        try { if (!process.Start()) throw new IOException("Unable to start secret-tool."); }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new IOException("Secret Service is unavailable. Install secret-tool and unlock the desktop keyring, or explicitly select local AES storage.", ex);
        }
        using var timeout = new CancellationTokenSource(_requestTimeout);
        var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            if (token is not null) await process.StandardInput.WriteAsync(token.AsMemory(), timeout.Token).ConfigureAwait(false);
            process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            var output = await outputTask.ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);
            if (process.ExitCode != 0)
            {
                if (operation is "lookup" or "clear" && process.ExitCode == 1 && string.IsNullOrWhiteSpace(error)) return null;
                throw new IOException("Secret Service request failed. Check the session D-Bus and unlock the desktop keyring.");
            }
            return operation == "lookup" ? output.TrimEnd('\r', '\n') : null;
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            if (!process.HasExited) process.Kill(true);
            await process.WaitForExitAsync().ConfigureAwait(false);
            try { await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            throw new TimeoutException("Secret Service request timed out.");
        }
    }
}
