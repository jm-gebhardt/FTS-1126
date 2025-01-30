using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to abort an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="AbortReason">The reason for the abort.</param>
public record AbortActionGroup(Guid CorrelationId, AbortReason AbortReason) : CorrelatedBy<Guid>;