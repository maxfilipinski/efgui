using System.Security.Cryptography;
using System.Text;

namespace EfGui.Core.Settings;

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

    // False when the value is encrypted but cannot be decrypted here (another
    // user/machine, or corrupted); the caller must keep the stored value intact.
    public static bool TryUnprotect(string stored, out string plaintext)
    {
        plaintext = stored;
        if (string.IsNullOrEmpty(stored) || !stored.StartsWith(Marker, StringComparison.Ordinal))
            return true;

        if (!OperatingSystem.IsWindows())
        {
            plaintext = "";
            return false;
        }

        try
        {
            var bytes = Convert.FromBase64String(stored[Marker.Length..]);
            plaintext = Encoding.UTF8.GetString(
                ProtectedData.Unprotect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser));
            return true;
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            plaintext = "";
            return false;
        }
    }
}
