using System.Security.Cryptography;
using System.Text;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Utilities;

/// <summary>
/// Extension methods to strings.
/// </summary>
[PublicAPI]
public static class StringExtensions
{
    /// <summary>
    /// Extension block for <see cref="string"/>.
    /// </summary>
    extension(string str)
    {
        /// <summary>
        /// Build GUID from string.
        /// </summary>
        /// <returns>Unique GUID according to the string.</returns>
        [PublicAPI]
        public Guid ToGuid()
        {
            if (Guid.TryParse(str, out var guid))
            {
                return guid;
            }

            byte[] stringBytes = Encoding.UTF8.GetBytes(str);
            // Usually SHA-1 is faster than MD5 and other SHA-algorithms.
            byte[] hashedBytes = SHA1.HashData(stringBytes);
            Array.Resize(ref hashedBytes, 16);
            return new Guid(hashedBytes);
        }

        /// <summary>
        /// Converts a string to a <see cref="GraphNode"/> object.
        /// </summary>
        /// <returns>A <see cref="GraphNode"/> instance initialized with the specified name and Id from <see cref="ToGuid"/>.</returns>
        public GraphNode ToGraphNode() => new(str.ToGuid(), str);

        /// <summary>
        /// Converts a string to a <see cref="BaseNode"/> object.
        /// </summary>
        /// <returns>A <see cref="BaseNode"/> instance initialized with the specified name and Id from <see cref="ToGuid"/>.</returns>
        public BaseNode ToBaseNode() => new(str.ToGuid(), str);
    }

    /// <summary>
    /// Extension block for string[].
    /// </summary>
    extension(string[]? stringArray)
    {
        /// <summary>
        /// Converts an array of strings to an array of GUIDs by using the <see cref="ToGuid"/> method.
        /// If the input array is null, it treats it as an empty array.
        /// </summary>
        [PublicAPI]
        public Guid[] ToGuidArray() => (stringArray ?? []).Select(s => s.ToGuid()).ToArray();
    }
}