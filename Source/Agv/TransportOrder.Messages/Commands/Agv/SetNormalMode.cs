using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to set the normal mode of an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Setter">The user that set the mode.</param>
public record SetNormalMode(Guid CorrelationId, string? Setter) : CorrelatedBy<Guid>;