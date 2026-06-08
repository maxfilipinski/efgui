using System;
using System.Security.Cryptography;
using System.Text;

namespace EfGui.Profiles;

// Protects sensitive profile fields (connection strings, which often carry
// passwords) at rest. Uses Windows DPAPI scoped to the current user; on other
// platforms, or if DPAPI fails, values are stored as-is.
public static class Secret
{
    private const string Marker = "enc:";

    public static string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext) || !OperatingSystem.IsWindows())
            return plaintext;

        try
        {
            var bytes = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(plaintext), optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Marker + Convert.ToBase64String(bytes);
        }
        catch (CryptographicException)
        {
            return plaintext;
        }
    }

    public static string Unprotect(string stored)
    {
        if (string.IsNullOrEmpty(stored) || !stored.StartsWith(Marker, StringComparison.Ordinal))
            return stored;

        if (!OperatingSystem.IsWindows())
            return stored;

        try
        {
            var bytes = Convert.FromBase64String(stored[Marker.Length..]);
            var plaintext = ProtectedData.Unprotect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plaintext);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            // Encrypted by another user/machine, or corrupted: surface the marker
            // rather than silently treating ciphertext as a connection string.
            return "";
        }
    }
}
