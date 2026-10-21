using System.Security.Cryptography;
using System.Text;

namespace LIS2.App.Avalonia;

/// <summary>
/// Local encrypted token storage. On Unix the key and ciphertext are restricted
/// to the current user (0600); neither is part of the exported workspace.
/// This protects against accidental disclosure, not compromise of the user account.
/// </summary>
public sealed class LegacyHaTokenStore
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
            using (var stream = new FileStream(keyPath, PrivateFileOptions(FileMode.CreateNew)))
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
            using (var stream = new FileStream(temp, PrivateFileOptions(FileMode.Create)))
            {
                Restrict(temp);
                stream.Write(data);
            }
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
        if (!File.Exists(tokenPath)) return null;
        if (!File.Exists(keyPath)) throw new CryptographicException("Invalid Home Assistant token storage.");
        ValidatePermissions(keyPath);
        ValidatePermissions(tokenPath);
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

    private static FileStreamOptions PrivateFileOptions(FileMode mode)
    {
        var options = new FileStreamOptions { Mode = mode, Access = FileAccess.Write, Share = FileShare.None };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        return options;
    }

    private static void ValidatePermissions(string path)
    {
        if (!OperatingSystem.IsWindows() && (File.GetUnixFileMode(path) &
            (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
             UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0)
            throw new CryptographicException("Home Assistant token files must be private to the current user.");
    }
}
