namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Represents the event raised when an automatic action has been aborted or canceled.
/// </summary>
/// <param name="TargetNodeId">The target node id.</param>
public record AutomaticActionWithdrawn(string TargetNodeId);