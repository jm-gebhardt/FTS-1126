using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Abort the currently running charge action.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record AbortChargeAction(Guid CorrelationId) : CorrelatedBy<Guid>;