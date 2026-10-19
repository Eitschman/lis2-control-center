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

    public PortableHaTokenStore(string? directory = null, string secretTool = "secret-tool")
    {
        _directory = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LIS2ControlCenter");
        _legacy = new LegacyHaTokenStore(_directory);
        _secretTool = secretTool;
    }

    public void Save(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        if (OperatingSystem.IsWindows())
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
            try { RunSecretTool("store", token); }
            catch (Exception ex) when (IsSecretServiceUnavailable(ex))
            {
                _legacy.Save(token);
                return;
            }
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
        string? native;
        try { native = LoadNative(); }
        catch (Exception ex) when (OperatingSystem.IsLinux() && IsSecretServiceUnavailable(ex))
        {
            return _legacy.Load();
        }
        if (!string.IsNullOrEmpty(native)) return native;
        var legacy = _legacy.Load();
        if (string.IsNullOrEmpty(legacy)) return null;
        // Do not break startup when the desktop keyring is unavailable/locked:
        // keep the old encrypted token until a future successful migration.
        try { Save(legacy); }
        catch (Exception) when (OperatingSystem.IsLinux()) { }
        return legacy;
    }

    public void Delete()
    {
        if (OperatingSystem.IsWindows())
        {
            var path = Path.Combine(_directory, "ha-token.dpapi");
            if (File.Exists(path)) File.Delete(path);
        }
        else if (OperatingSystem.IsLinux())
        {
            try { RunSecretTool("clear", null); }
            catch (Exception ex) when (IsSecretServiceUnavailable(ex)) { }
        }
        _legacy.Delete();
        var key = Path.Combine(_directory, "ha-token.key");
        if (File.Exists(key)) File.Delete(key);
    }

    private static bool IsSecretServiceUnavailable(Exception ex) =>
        ex is System.ComponentModel.Win32Exception or TimeoutException or IOException;

    private string? LoadNative()
    {
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

    private string? RunSecretTool(string operation, string? token)
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
        using var process = Process.Start(psi) ?? throw new IOException("Unable to start secret-tool.");
        if (token is not null)
        {
            process.StandardInput.Write(token);
            process.StandardInput.Close();
        }
        else process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(10000))
        {
            process.Kill(true);
            throw new TimeoutException("Secret Service request timed out.");
        }
        if (process.ExitCode != 0)
        {
            if (operation == "lookup" && string.IsNullOrWhiteSpace(error)) return null;
            throw new IOException("Secret Service request failed (secret-tool exit " + process.ExitCode + ").");
        }
        return operation == "lookup" ? output.TrimEnd('\r', '\n') : null;
    }
}