using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.MessageContent;

/// <summary>
/// Message content for a graph edge.
/// </summary>
[UsedImplicitly]
public record GraphEdge
{
    /// <summary>The from node.</summary>
    public GraphNode From { get; init; }

    /// <summary>The to node.</summary>
    public GraphNode To { get; init; }

    /// <summary>The variant of this edge.</summary>
    public EdgeType Variant { get; init; }

    /// <summary>Parameters for the variant type of this edge.</summary>
    public ICollection<EdgeCoefficient> VariantParameters { get; init; }

    /// <summary>The length of the edge.</summary>
    public double Length { get; init; }

    /// <summary>
    /// AutoMapper
    /// </summary>
    private GraphEdge()
    {
        From = null!;
        To = null!;
        VariantParameters = null!;
    }

    /// <inheritdoc cref="GraphEdge"/>
    /// <param name="from">The from node.</param>
    /// <param name="to">The to node.</param>
    /// <param name="variant">The variant of this edge.</param>
    /// <param name="variantParameters">Parameters for the variant type of this edge.</param>
    /// <param name="length">The length of the edge.</param>
    public GraphEdge(GraphNode from, GraphNode to, EdgeType variant, ICollection<EdgeCoefficient> variantParameters, double length = 1.0)
    {
        From = from;
        To = to;
        Variant = variant;
        VariantParameters = variantParameters;
        Length = length;
    }
}