using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;

/// <summary>
/// Represents the event raised when an action group got cancelled.
/// </summary>
/// <param name="ActionGroup">The action group.</param>
/// <param name="CorrelationId">The correlation id of the action group.</param>
public record ActionGroupCancelled(ActionGroup ActionGroup, Guid CorrelationId) : CorrelatedBy<Guid>, IActionGroupEvent;