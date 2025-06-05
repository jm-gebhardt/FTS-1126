using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to handle the completion of an action at the agv.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="IsQueueing">True, if the agv is queueing.</param>
public record HandleActionCompletion(Guid CorrelationId, bool IsQueueing=false) : CorrelatedBy<Guid>;