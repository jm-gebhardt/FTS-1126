using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Represents a command to resend an action to the agv.
/// </summary>
/// <param name="CorrelationId">Correlation id of the agv.</param>
/// <param name="User">The user who wants to resend current action to the agv.</param>
public record ResendAction(Guid CorrelationId, string User): CorrelatedBy<Guid>;