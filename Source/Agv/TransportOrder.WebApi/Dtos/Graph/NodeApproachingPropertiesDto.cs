using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;

/// <summary>
/// Provides a dto for node approaching parameters.
/// </summary>
[PublicAPI]
public record NodeApproachingPropertiesDto : DatedMessage
{
    /// <summary>
    /// The correlation id of the map.
    /// </summary>
    public Guid MapCorrelationId { get; set; }

    /// <summary >
    /// The node id.
    /// </summary>
    public string NodeId { get; set; }

    /// <summary>
    /// The drop lift height, if set.
    /// </summary>
    public double? DropLiftHeight { get; set; }

    /// <summary>
    /// The pick lift height, if set.
    /// </summary>
    public double? PickLiftHeight { get; set; }

    /// <summary>
    /// The docking direction of the node, if set.
    /// </summary>
    public DockingDirection? DockingDirection { get; set; }

    /// <inheritdoc cref="NodeApproachingPropertiesDto"/>>
    public NodeApproachingPropertiesDto(Guid mapCorrelationId, string nodeId, double? dropLiftHeight, double? pickLiftHeight, DockingDirection? dockingDirection)
    {
        MapCorrelationId = mapCorrelationId;
        NodeId = nodeId;
        DropLiftHeight = dropLiftHeight;
        PickLiftHeight = pickLiftHeight;
        DockingDirection = dockingDirection;
    }
}