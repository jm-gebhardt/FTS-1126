using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Events;

/// <summary>
/// An event reporting that an action group was created
/// </summary>
/// <param name="ActionGroup">Action group</param>
/// <param name="CorrelationId">The correlationId of the action group</param>
/// <param name="AssignedAgv"> The agv assigned to the action group</param>
[PublicAPI]
public record ActionGroupCreated(ActionGroup ActionGroup, Guid CorrelationId, Guid? AssignedAgv) : CorrelatedBy<Guid>, IActionGroupEvent;