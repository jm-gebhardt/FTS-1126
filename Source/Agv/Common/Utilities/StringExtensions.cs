using System.Text;


namespace Gebhardt.StoreWare.Agv.Common.Utilities
{
    /// <summary>
    /// Extension methods to strings.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public static class StringExtensions
    {
        /// <summary>
        /// Build GUID from string.
        /// </summary>
        /// <param name="str">Input string</param>
        /// <returns>Unique GUID according to <paramref name="str"/>.</returns>
        [JetBrains.Annotations.PublicAPI]
        public static Guid ToGuid(this string str)
        {
            byte[] stringbytes = Encoding.UTF8.GetBytes(str);
            byte[] hashedBytes;
            // Usually SHA-1 is faster than MD5 and other SHA-algos.
            using (var sha = System.Security.Cryptography.SHA1.Create())
            {
                hashedBytes = sha.ComputeHash(stringbytes);
            }
            Array.Resize(ref hashedBytes, 16);
            return new Guid(hashedBytes);
        }
    }
}
