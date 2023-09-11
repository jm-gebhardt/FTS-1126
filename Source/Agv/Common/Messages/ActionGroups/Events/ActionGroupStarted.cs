using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;

/// <summary>
/// Represents the event raised when the first action of an action group has been started.
/// </summary>
/// <param name="ActionGroup">The action group.</param>
/// <param name="CorrelationId">The correlationId of the action group.</param>
public record ActionGroupStarted(ActionGroup ActionGroup, Guid CorrelationId) : CorrelatedBy<Guid>, IActionGroupEvent;