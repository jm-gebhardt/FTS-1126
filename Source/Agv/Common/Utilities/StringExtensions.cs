using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Utilities;

/// <summary>
/// Extension methods to strings.
/// </summary>
[PublicAPI]
public static class StringExtensions
{
    /// <summary>
    /// Build GUID from string.
    /// </summary>
    /// <param name="str">Input string</param>
    /// <returns>Unique GUID according to <paramref name="str"/>.</returns>
    [PublicAPI]
    public static Guid ToGuid(this string str)
    {
        if (Guid.TryParse(str, out var guid))
        {
            return guid;
        }

        byte[] stringBytes = Encoding.UTF8.GetBytes(str);
        byte[] hashedBytes;
        // Usually SHA-1 is faster than MD5 and other SHA-algorithms.
        using (var sha = SHA1.Create())
        {
            hashedBytes = sha.ComputeHash(stringBytes);
        }
        Array.Resize(ref hashedBytes, 16);
        return new Guid(hashedBytes);
    }
}