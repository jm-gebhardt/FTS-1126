using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;

/// <summary>
/// An edge in the graph connects two nodes. This model is for EF only
/// </summary>
public class EfEdge
{
    /// <summary>
    /// The Id of the edge
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The Id of the starting node of the edge
    /// </summary>
    public string FromNodeId { get; init; }

    /// <summary>
    /// The Id of the ending node of the edge
    /// </summary>
    public string ToNodeId { get; init; }

    /// <summary>
    /// The Id of the parent graph
    /// </summary>
    [UsedImplicitly]
    public Guid GraphCorrelationId { get; init; }

    /// <summary>
    /// Distance/costs to travel this edge (unitless quantifier).
    /// </summary>
    public float Metric { get; init; }

    /// <summary>
    /// The variant of this edge.
    /// </summary>
    public EdgeType Variant { get; init; }

    /// <summary>
    /// Parameters for the variant type of this edge.
    /// </summary>
    public ICollection<EdgeCoefficient> VariantParameters { get; init; }

    /// <summary>
    /// Ctor
    /// </summary>
    [UsedImplicitly]
    private EfEdge()
    {
        FromNodeId = null!;
        ToNodeId = null!;
        Variant = EdgeType.Direct;
        VariantParameters = new List<EdgeCoefficient>();
    }
}