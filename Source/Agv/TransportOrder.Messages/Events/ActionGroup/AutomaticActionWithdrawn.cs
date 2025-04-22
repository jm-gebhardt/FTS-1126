namespace Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Represents the event raised when an automatic action has been aborted or canceled.
/// </summary>
/// <param name="TargetNodeId">The target node id.</param>
/// <param name="ReservedOnly">If true, free the node only if it was reserved.</param>
public record AutomaticActionWithdrawn(string TargetNodeId, bool ReservedOnly=false);