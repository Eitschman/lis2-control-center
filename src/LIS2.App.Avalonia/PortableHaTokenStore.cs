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
        var native = LoadNative();
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
            RunSecretTool("clear", null);
        }
        _legacy.Delete();
        var key = Path.Combine(_directory, "ha-token.key");
        if (File.Exists(key)) File.Delete(key);
    }

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

using System.Security.Cryptography;
using System.Text;

namespace LIS2.App.Avalonia;

/// <summary>
/// Local encrypted token storage. On Unix the key and ciphertext are restricted
/// to the current user (0600); neither is part of the exported workspace.
/// This protects against accidental disclosure, not compromise of the user account.
/// </summary>
internal sealed class LegacyHaTokenStore
{
    private readonly string _directory;
    public LegacyHaTokenStore(string? directory = null) =>
        _directory = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LIS2ControlCenter");

    public void Save(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        Directory.CreateDirectory(_directory);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(_directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var keyPath = Path.Combine(_directory, "ha-token.key");
        byte[] key;
        if (File.Exists(keyPath))
            key = File.ReadAllBytes(keyPath);
        else
        {
            key = RandomNumberGenerator.GetBytes(32);
            using (var stream = new FileStream(keyPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                stream.Write(key);
            Restrict(keyPath);
        }
        if (key.Length != 32) throw new CryptographicException("Invalid Home Assistant token key.");
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(token);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        try
        {
            using (var aes = new AesGcm(key, 16)) aes.Encrypt(nonce, plaintext, ciphertext, tag);
            var data = new byte[1 + nonce.Length + tag.Length + ciphertext.Length];
            data[0] = 1;
            nonce.CopyTo(data, 1);
            tag.CopyTo(data, 13);
            ciphertext.CopyTo(data, 29);
            var temp = Path.Combine(_directory, "ha-token.bin.tmp");
            File.WriteAllBytes(temp, data);
            Restrict(temp);
            File.Move(temp, Path.Combine(_directory, "ha-token.bin"), true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public string? Load()
    {
        var keyPath = Path.Combine(_directory, "ha-token.key");
        var tokenPath = Path.Combine(_directory, "ha-token.bin");
        if (!File.Exists(keyPath) || !File.Exists(tokenPath)) return null;
        var key = File.ReadAllBytes(keyPath);
        var data = File.ReadAllBytes(tokenPath);
        if (key.Length != 32 || data.Length < 29 || data[0] != 1)
            throw new CryptographicException("Invalid Home Assistant token storage.");
        var plaintext = new byte[data.Length - 29];
        try
        {
            using (var aes = new AesGcm(key, 16)) aes.Decrypt(data.AsSpan(1, 12), data.AsSpan(29), data.AsSpan(13, 16), plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public void Delete()
    {
        var path = Path.Combine(_directory, "ha-token.bin");
        if (File.Exists(path)) File.Delete(path);
    }

    private static void Restrict(string path)
    {
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}
