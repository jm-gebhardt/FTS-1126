using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Represents the request to try to set the approaching properties for a node.
/// </summary>
/// <param name="MapId">The map id.</param>
/// <param name="NodeId">The node id.</param>
/// <param name="PickLiftHeight">The lift height for picking.</param>
/// <param name="DropLiftHeight">The lift height for dropping.</param>
/// <param name="DockingDirection">The docking direction.</param>
public record TrySettingNodeApproachingProperties(Guid MapId, string NodeId, double? PickLiftHeight, double? DropLiftHeight, DockingDirection? DockingDirection);