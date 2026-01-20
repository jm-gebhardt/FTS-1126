using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

/// <summary>
/// Provides a request to mark a trigger for deletion (soft delete).
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
public record TryMarkTriggerForDeletion(Guid TriggerId) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => TriggerId;
}