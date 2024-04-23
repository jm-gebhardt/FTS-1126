using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to handle the completion of an action at the agv.
/// </summary>
/// <param name="CorrelationId"></param>
public record HandleActionCompletion(Guid CorrelationId) : CorrelatedBy<Guid>;