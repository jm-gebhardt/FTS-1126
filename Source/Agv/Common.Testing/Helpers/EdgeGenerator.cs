using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Helpers;

/// <summary>
/// Generator for <see cref="GraphEdge"/>
/// </summary>
public static class EdgeGenerator
{
    public static GraphEdge Generate(string fromNodeId = "from",
        string toNodeId = "to",
        Guid? id = null,
        double length = 1,
        EdgeType variant = EdgeType.Direct,
        ICollection<EdgeCoefficient>? variantParameters = null)
    {
        return new GraphEdge
        {
            FromNodeId = fromNodeId,
            ToNodeId = toNodeId,
            Id = id ?? Guid.NewGuid(),
            Length = length,
            Variant = variant,
            VariantParameters = variantParameters ?? new List<EdgeCoefficient>()
        };
    }
}