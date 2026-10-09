using System.Security.Cryptography;
using System.Text;

namespace EfGui.Core.Settings;

// Encrypts connection strings at rest with per-user DPAPI; stored as-is outside Windows.
public static class Secret
{
    private const string Marker = "enc:";

    // False only when DPAPI failed on Windows and the value stays plain text.
    public static bool TryProtect(string plaintext, out string stored)
    {
        stored = plaintext;
        if (string.IsNullOrEmpty(plaintext) || !OperatingSystem.IsWindows())
        {
            return true;
        }

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

    // False when it can't be decrypted here (other user/machine, corrupted); keep the stored value.
    public static bool TryUnprotect(string stored, out string plaintext)
    {
        plaintext = stored;
        if (string.IsNullOrEmpty(stored) || !stored.StartsWith(Marker, StringComparison.Ordinal))
        {
            return true;
        }

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
