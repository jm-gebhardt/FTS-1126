namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Node;

/// <summary>
/// Provides a command to free a node.
/// </summary>
/// <param name="NodeId">The node id.</param>
public record FreeNode(string NodeId);