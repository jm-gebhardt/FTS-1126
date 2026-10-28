using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// The runtime state of a node.
/// </summary>
/// <param name="Occupancy">Traffic management state: Whether the node is free, reserved or occupied by an AGV.</param>
/// <param name="AgvId">The id of the AGV the node is reserved for or occupied by.</param>
/// <param name="Lock">The lock set by an operator. Null if the node is not locked.</param>
/// <param name="ModifiedDate">The time of the last change. Null if the state never changed.</param>
[PublicAPI]
public record NodeStatusDto(OccupancyState Occupancy, Guid? AgvId, LockDto? Lock, DateTime? ModifiedDate);