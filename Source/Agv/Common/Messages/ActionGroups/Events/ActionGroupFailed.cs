using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;

/// <summary>
/// Represents the event raised when an action group failed.
/// </summary>
/// <param name="ActionGroup">Action group.</param>
/// <param name="CorrelationId">The correlationId of the action group.</param>
/// <param name="ErrorCode">The error code of the failure.</param>
public record ActionGroupFailed(ActionGroup ActionGroup, Guid CorrelationId, HostOrderHandlingFailedErrorCode ErrorCode) : CorrelatedBy<Guid>, IActionGroupEvent;