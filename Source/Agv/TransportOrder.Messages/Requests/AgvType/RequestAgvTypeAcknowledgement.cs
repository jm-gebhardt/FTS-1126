using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.AgvType;

/// <summary>
/// Provides a request to acknowledge an agv type.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record RequestAgvTypeAcknowledgement(Guid CorrelationId) : CorrelatedBy<Guid>;