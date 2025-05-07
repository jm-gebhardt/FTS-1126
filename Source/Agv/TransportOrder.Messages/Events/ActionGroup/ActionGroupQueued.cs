using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Represents the event raised when an action group has been queued.
/// </summary>
/// <param name="ActionGroup">Action group</param>
/// <param name="CorrelationId">The correlationId of the action group</param>
/// <param name="AssignedAgv"> The agv assigned to the action group</param>
[PublicAPI]
public record ActionGroupQueued(Common.Messages.ActionGroups.ActionGroup ActionGroup, Guid CorrelationId, Guid? AssignedAgv);