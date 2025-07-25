using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.RestrictedArea;

/// <summary>
/// Represents the event raised when a restricted area has been deleted.
/// </summary>
/// <param name="CorrelationId">The correlation id of the restricted area.</param>
public record RestrictedAreaDeleted(Guid CorrelationId) : CorrelatedBy<Guid>;