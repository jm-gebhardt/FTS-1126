using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to try to reach the queue head at node.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="HeadNodeId">The head node id.</param>
public record TryReachQueueHead(Guid CorrelationId, string? HeadNodeId): CorrelatedBy<Guid>;