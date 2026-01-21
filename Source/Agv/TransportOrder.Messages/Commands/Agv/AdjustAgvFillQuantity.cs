using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to adjust the agv fill quantity of a queue.
/// </summary>
/// <param name="QueueId">Queue to adjust.</param>
/// <param name="QueueName">Name of queue.</param>
public record AdjustAgvFillQuantity(Guid QueueId, string QueueName = "") : CorrelatedBy<string>
{
    /// <inheritdoc />
    public string CorrelationId => QueueName;
}