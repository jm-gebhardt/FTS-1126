using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Map;

/// <summary>
/// Represents the event raised when a nodes approaching properties have been updated.
/// </summary>
/// <param name="MapId">The map id.</param>
/// <param name="NodeId">The node id.</param>
/// <param name="PickLiftHeight">The lift height for picking.</param>
/// <param name="DropLiftHeight">The lift height for dropping.</param>
/// <param name="DockingDirection">The docking direction.</param>
public record NodeApproachingPropertiesUpdated(Guid MapId, string NodeId, double? PickLiftHeight, double? DropLiftHeight, DockingDirection? DockingDirection);