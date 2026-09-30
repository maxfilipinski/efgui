using System.Security.Cryptography;
using System.Text;

namespace EfGui.Core.Settings;

// Protects sensitive profile fields (connection strings, which often carry
// passwords) at rest. Uses Windows DPAPI scoped to the current user; on other
// platforms, or if DPAPI fails, values are stored as-is.
public static class Secret
{
    private const string Marker = "enc:";

    // False only when encryption was expected but DPAPI failed, so the value is stored
    // as plain text; outside Windows plain text is the documented behavior.
    public static bool TryProtect(string plaintext, out string stored)
    {
        stored = plaintext;
        if (string.IsNullOrEmpty(plaintext) || !OperatingSystem.IsWindows())
            return true;

        try
        {
            var bytes = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(plaintext), optionalEntropy: null, DataProtectionScope.CurrentUser);
            stored = Marker + Convert.ToBase64String(bytes);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
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
