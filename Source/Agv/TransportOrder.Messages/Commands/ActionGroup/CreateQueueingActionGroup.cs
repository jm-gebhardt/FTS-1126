using Gebhardt.Acc.Common.Domain.Models.Graphs;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to create a queueing action group.
/// </summary>
/// <param name="CorrelationId">The action group correlation id.</param>
/// <param name="AgvCorrelationId">The agv correlation id.</param>
/// <param name="QueueNode">The queue node.</param>
public record CreateQueueingActionGroup(Guid CorrelationId, Guid AgvCorrelationId, GraphNode QueueNode) : CorrelatedBy<Guid>;