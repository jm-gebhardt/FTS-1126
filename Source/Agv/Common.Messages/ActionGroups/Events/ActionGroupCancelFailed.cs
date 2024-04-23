using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Events;

/// <summary>
/// Represents the event raised when an action group cancelling failed.
/// </summary>
/// <param name="ActionGroup">The action group.</param>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="ErrorCode"> The error code of the failure.</param>
public record ActionGroupCancelFailed(ActionGroup ActionGroup, Guid CorrelationId, HostOrderCancelFailedErrorCode ErrorCode) : CorrelatedBy<Guid>, IActionGroupEvent;