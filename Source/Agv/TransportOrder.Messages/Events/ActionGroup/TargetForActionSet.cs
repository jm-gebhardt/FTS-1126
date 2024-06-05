using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Represents the event raised when a target for an action has been set.
/// </summary>
/// <param name="Action">The action.</param>
/// <param name="AssignedAgv">The agv (if available).</param>
public record TargetForActionSet(AgvAction Action, Guid? AssignedAgv);