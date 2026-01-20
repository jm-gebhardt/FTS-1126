using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Trigger;

/// <summary>
/// Represents the event raised when a trigger is restored from deletion.
/// </summary>
/// <param name="Trigger">The trigger.</param>
public record TriggerRestored(Domain.Models.Trigger.Trigger Trigger) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => Trigger.Id;
}