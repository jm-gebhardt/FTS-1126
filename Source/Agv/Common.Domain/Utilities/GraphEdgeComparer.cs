using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Domain.Utilities;

/// <summary>
/// Comparer to check for equal edges according to source and destination.
/// </summary>
[PublicAPI]
public class GraphEdgeComparer : IEqualityComparer<GraphEdge>
{
    /// <inheritdoc />
    public bool Equals(GraphEdge? e1, GraphEdge? e2)
    {
        return e1 != null && e2 != null && e1.FromNodeId == e2.FromNodeId && e1.ToNodeId == e2.ToNodeId && e1.Variant == e2.Variant && e1.VariantParameters.SequenceEqual(e2.VariantParameters);
    }

    /// <inheritdoc />
    public int GetHashCode(GraphEdge e)
    {
        int hash = 0;

        foreach (var edgeCoefficient in e.VariantParameters)
        {
            hash = HashCode.Combine(hash, edgeCoefficient);
        }

        return HashCode.Combine(e.FromNodeId, e.ToNodeId, e.Variant, hash);
    }
}