using Gebhardt.Acc.TransportOrder.Domain.Models.Node;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Map;

/// <summary>
/// Represents the event raised when the state of a node changed.
/// </summary>
/// <param name="NodeState">The node state.</param>
/// <param name="StationId">The id of the station.</param>
public record NodeStateChanged(NodeLock NodeState, Guid? StationId);