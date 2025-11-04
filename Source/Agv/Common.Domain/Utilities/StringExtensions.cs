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
        // Usually SHA-1 is faster than MD5 and other SHA-algorithms.
        byte[] hashedBytes = SHA1.HashData(stringBytes);
        Array.Resize(ref hashedBytes, 16);
        return new Guid(hashedBytes);
    }

    /// <summary>
    /// Converts a string to a <see cref="GraphNode"/> object.
    /// </summary>
    /// <param name="nodeName">The name of the node to convert.</param>
    /// <returns>A <see cref="GraphNode"/> instance initialized with the specified name and Id from <see cref="ToGuid"/>.</returns>
    public static GraphNode ToGraphNode(this string nodeName) => new(nodeName.ToGuid(), nodeName);

    /// <summary>
    /// Converts a string to a <see cref="BaseNode"/> object.
    /// </summary>
    /// <param name="nodeName">The name of the node to convert.</param>
    /// <returns>A <see cref="BaseNode"/> instance initialized with the specified name and Id from <see cref="ToGuid"/>.</returns>
    public static BaseNode ToBaseNode(this string nodeName) => new(nodeName.ToGuid(), nodeName);
}