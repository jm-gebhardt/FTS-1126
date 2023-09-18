using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Utilities;

/// <summary>
/// Comparer to check for equal edges according to source and destination
/// </summary>
[PublicAPI]
public class GraphEdgeComparer : IEqualityComparer<GraphEdge>
{
    /// <inheritdoc />
    public bool Equals(GraphEdge? e1, GraphEdge? e2)
    {
        return e1 != null && e2 != null && e1.FromNodeId == e2.FromNodeId && e1.ToNodeId == e2.ToNodeId;
    }

    /// <inheritdoc />
    public int GetHashCode(GraphEdge e)
    {
        return HashCode.Combine(e.FromNodeId, e.ToNodeId);
    }
}