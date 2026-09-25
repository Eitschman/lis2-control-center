using System.Security.Cryptography;
using System.Text;

namespace LIS2.App;

internal static class HomeAssistantTokenProtector
{
    private const string Prefix = "dpapi:v1:";

    private static readonly byte[] Entropy =
        Encoding.UTF8.GetBytes("LIS2ControlCenter.HomeAssistant.AccessToken.v1");

    public static string Protect(string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
            return string.Empty;

        var plaintext = Encoding.UTF8.GetBytes(accessToken);
        try
        {
            var protectedBytes = ProtectedData.Protect(
                plaintext,
                Entropy,
                DataProtectionScope.CurrentUser);

            try
            {
                return Prefix + Convert.ToBase64String(protectedBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(protectedBytes);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public static string UnprotectOrMigrateLegacy(string? persistedValue)
    {
        if (string.IsNullOrEmpty(persistedValue))
            return string.Empty;

        if (!persistedValue.StartsWith("dpapi:", StringComparison.OrdinalIgnoreCase))
            return persistedValue;

        if (!persistedValue.StartsWith(Prefix, StringComparison.Ordinal))
            return string.Empty;

        try
        {
            var protectedBytes = Convert.FromBase64String(
                persistedValue[Prefix.Length..]);

            try
            {
                var plaintext = ProtectedData.Unprotect(
                    protectedBytes,
                    Entropy,
                    DataProtectionScope.CurrentUser);

                try
                {
                    return Encoding.UTF8.GetString(plaintext);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(protectedBytes);
            }
        }
        catch (FormatException)
        {
            return string.Empty;
        }
        catch (CryptographicException)
        {
            return string.Empty;
        }
    }

    internal static bool IsProtected(string? value) =>
        !string.IsNullOrEmpty(value) &&
        value.StartsWith(Prefix, StringComparison.Ordinal);
}
