using Gebhardt.Acc.TransportOrder.Domain.Models.Node;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Map;

/// <summary>
/// Represents the event raised when the state of a node changed.
/// </summary>
/// <param name="NodeState">The node state.</param>
public record NodeStateChanged(NodeLock NodeState);