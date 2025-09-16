using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.Common.Testing.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Converts an array of strings to an array of <see cref="GraphNode"/> objects.
    /// </summary>
    /// <param name="nodeNames">Array of node names to convert.</param>
    /// <returns>Array of <see cref="GraphNode"/> instances corresponding to the provided names.</returns>
    public static GraphNode[] ToGraphNodeArray(this string[] nodeNames) => nodeNames.Select(n => n.ToGraphNode()).ToArray();
}