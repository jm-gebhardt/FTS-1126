namespace Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Represents the event raised when an automatic action has been aborted or canceled.
/// </summary>
/// <param name="AgvCorrelationId">The agv that ran the action, in case it already started.</param>
/// <param name="TargetNodeId">The target node id.</param>
/// <param name="ReservedOnly">If true, free the node only if it was reserved.</param>
public record AutomaticActionWithdrawn(Guid? AgvCorrelationId, string TargetNodeId, bool ReservedOnly=false);