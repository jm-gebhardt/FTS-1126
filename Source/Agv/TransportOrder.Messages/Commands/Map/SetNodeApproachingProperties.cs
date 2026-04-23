using Gebhardt.Acc.Common.Domain.Models.Graphs;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Map;

/// <summary>
/// Provides a command to set the approaching properties for a node.
/// </summary>
/// <param name="MapId">The map id.</param>
/// <param name="NodeId">The node id.</param>
/// <param name="PickLiftHeight">The lift height for picking.</param>
/// <param name="DropLiftHeight">The lift height for dropping.</param>
/// <param name="DockingDirection">The docking direction.</param>
public record SetNodeApproachingProperties(Guid MapId, Guid NodeId, double? PickLiftHeight, double? DropLiftHeight, DockingDirection? DockingDirection) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{Guid}"/>
    public Guid CorrelationId => NodeId;
}