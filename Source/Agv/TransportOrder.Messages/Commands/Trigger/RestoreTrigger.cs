using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;

/// <summary>
/// Provides a command to restore a previously deleted trigger.
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
public record RestoreTrigger(Guid TriggerId) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => TriggerId;
}

