using JetBrains.Annotations;
using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.Common.Domain.Utilities;

/// <summary>
/// Comparer to check for equal node ID.
/// </summary>
[PublicAPI]
public class GraphNodeComparer : IEqualityComparer<GraphNode>
{
    /// <inheritdoc />
    public bool Equals(GraphNode? n1, GraphNode? n2)
    {
        return n1 != null && n2 != null && n1.Id == n2.Id;
    }

    /// <inheritdoc />
    public int GetHashCode(GraphNode n)
    {
        return n.Id.GetHashCode();
    }
}