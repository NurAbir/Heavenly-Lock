using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace HeavenlyLock.Helpers;

public static class SecureStringHelper
{
    public static byte[] ToByteArray(this SecureString secureString)
    {
        if (secureString == null)
            throw new ArgumentNullException(nameof(secureString));

        IntPtr unmanagedString = IntPtr.Zero;
        try
        {
            unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
            char[] chars = new char[secureString.Length];
            Marshal.Copy(unmanagedString, chars, 0, secureString.Length);
            return Encoding.UTF8.GetBytes(chars);
        }
        finally
        {
            if (unmanagedString != IntPtr.Zero)
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
        }
    }

    public static void SecureClear(this byte[] bytes)
    {
        if (bytes == null) return;
        CryptographicOperations.ZeroMemory(bytes);
    }
}
