using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

/// <summary>
/// Provides a request to restore a previously deleted trigger.
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
public record TryRestoreTrigger(Guid TriggerId) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => TriggerId;
}