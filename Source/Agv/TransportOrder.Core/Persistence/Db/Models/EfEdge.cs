using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;

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
    /// Ctor
    /// </summary>
    [UsedImplicitly]
    private EfEdge()
    {
        FromNodeId = null!;
        ToNodeId = null!;
    }
}