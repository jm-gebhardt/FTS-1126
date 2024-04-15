using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;

/// <summary>
/// Model for drawing an edge in the UI.
/// </summary>
[PublicAPI]
public record GraphEdgeDto
{
    /// <summary>
    /// Id of the node where the edge starts.
    /// </summary>
    public string FromNodeId { get; init; } = null!;

    /// <summary>
    /// Id of the node where the edge ends.
    /// </summary>
    public string ToNodeId { get; init; } = null!;

    /// <summary>
    /// Length of the edge in [m].
    /// </summary>
    public double Length { get; init; } = 1.0;

    /// <summary>
    /// Variant of this edge.
    /// </summary>
    public EdgeType Variant { get; init; } = EdgeType.Direct;

    /// <summary>
    /// Parameters required for the edge variant.
    /// </summary>
    public ICollection<EdgeCoefficient> VariantParameters { get; init; } = null!;
}