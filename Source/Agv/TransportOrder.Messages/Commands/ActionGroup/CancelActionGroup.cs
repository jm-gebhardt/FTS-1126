using MassTransit;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to cancel an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="CancelReason">The reason for the cancel.</param>
public record CancelActionGroup(Guid CorrelationId, CancelReason CancelReason) : CorrelatedBy<Guid>;