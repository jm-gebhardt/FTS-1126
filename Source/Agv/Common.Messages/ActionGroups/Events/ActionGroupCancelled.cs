using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;

/// <summary>
/// Represents the event raised when an action group got canceled.
/// </summary>
/// <param name="ActionGroup">The action group.</param>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="MessageCreatedDate">The time the action group was canceled.</param>
public record ActionGroupCanceled(ActionGroup ActionGroup, Guid CorrelationId, DateTime MessageCreatedDate) : CorrelatedBy<Guid>, IActionGroupEvent, IMessageCreatedDate;