using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Represents the event raise, when a charge action has been completed.
/// </summary>
/// <param name="ActionGroupCorrelationId">The action group correlation id.</param>
/// <param name="ActionCorrelationId">The action correlation id.</param>
[PublicAPI]
public record ChargeActionCompleted(Guid ActionGroupCorrelationId, Guid ActionCorrelationId) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => ActionCorrelationId;
}