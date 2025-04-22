namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Node;

/// <summary>
/// Provides a command to free a node.
/// </summary>
/// <param name="NodeId">The node id.</param>
/// <param name="ReservedOnly">If true, free the node only if it was reserved.</param>
public record FreeNode(string NodeId, bool ReservedOnly = false);