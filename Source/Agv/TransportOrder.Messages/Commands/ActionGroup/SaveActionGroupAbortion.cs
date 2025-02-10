using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to store the abortion of an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="AbortedActionId">The action that was aborted.</param>
/// <param name="AbortReason">The reason for the abort.</param>
public record SaveActionGroupAbortion(Guid CorrelationId, Guid AbortedActionId, AbortReason AbortReason) : CorrelatedBy<Guid>;