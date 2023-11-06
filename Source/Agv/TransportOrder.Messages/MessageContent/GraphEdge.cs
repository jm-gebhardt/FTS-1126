using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;

/// <summary>
/// Message content for a graph edge.
/// </summary>
/// <param name="From">The from node.</param>
/// <param name="To">The to node.</param>
/// <param name="Variant">The variant of this edge.</param>
/// <param name="VariantParameters">Parameters for the variant type of this edge.</param>
/// <param name="Length">The length of the edge.</param>
[UsedImplicitly]
public record GraphEdge(GraphNode From, GraphNode To, EdgeType Variant, ICollection<EdgeCoefficient> VariantParameters, double Length = 1.0);