using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;

/// <summary>
/// Provides a command to mark a trigger for deletion (soft delete).
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
public record MarkTriggerForDeletion(Guid TriggerId) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => TriggerId;
}
