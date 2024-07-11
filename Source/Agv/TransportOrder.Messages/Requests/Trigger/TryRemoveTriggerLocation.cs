using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

/// <summary>
/// Provides a command to remove a trigger location.
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
/// <param name="LocationId">The trigger location id.</param>
public record TryRemoveTriggerLocation(Guid TriggerId, Guid LocationId) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => TriggerId;
}