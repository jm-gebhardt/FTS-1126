using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to create a queueing action group.
/// </summary>
/// <param name="CorrelationId">The action group correlation id.</param>
/// <param name="AgvCorrelationId">The agv correlation id.</param>
/// <param name="QueueNodeId">The queue node id.</param>
public record CreateQueueingActionGroup(Guid CorrelationId, Guid AgvCorrelationId, string QueueNodeId) : CorrelatedBy<Guid>;