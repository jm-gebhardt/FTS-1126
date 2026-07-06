using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.AgvType;

/// <summary>
/// Represents the event raised, when an agv type has been acknowledged.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv type.</param>
public record AgvTypeAcknowledged(Guid CorrelationId) : CorrelatedBy<Guid>;