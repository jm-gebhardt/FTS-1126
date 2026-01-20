using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Trigger;

/// <summary>
/// Represents the event raised when a trigger is marked for deletion (soft delete).
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
public record TriggerMarkedForDeletion(Guid TriggerId) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => TriggerId;
}
