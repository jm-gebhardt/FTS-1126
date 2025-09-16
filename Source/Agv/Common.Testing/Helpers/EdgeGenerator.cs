using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.Common.Testing.Helpers;

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
        ICollection<EdgeCoefficient>? variantParameters = null,
        DateTime timeStamp = default
        )
    {
        if (timeStamp == default)
        {
            timeStamp = DateTime.UtcNow;
        }
        return new GraphEdge
        {
            FromNodeId = fromNodeId.ToGuid(),
            ToNodeId = toNodeId.ToGuid(),
            Id = id ?? Guid.NewGuid(),
            Length = length,
            Variant = variant,
            VariantParameters = variantParameters ?? new List<EdgeCoefficient>(),
            TimeStamp = timeStamp
        };
    }
}