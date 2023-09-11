using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;

/// <summary>
/// Represents the event raised when the all actions of an action group have been completed.
/// </summary>
/// <param name="ActionGroup">The action group.</param>
/// <param name="CorrelationId">The correlationId of the action group.</param>
public record ActionGroupCompleted(ActionGroup ActionGroup, Guid CorrelationId) : CorrelatedBy<Guid>, IActionGroupEvent;