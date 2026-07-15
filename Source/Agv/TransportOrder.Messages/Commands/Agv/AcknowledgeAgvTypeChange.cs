using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to acknowledge a type change of the agv.
/// </summary>
public record AcknowledgeAgvTypeChange(Guid CorrelationId) : CorrelatedBy<Guid>;