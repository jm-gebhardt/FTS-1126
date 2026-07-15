using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;

/// <summary>
/// Represents the event raised when the change of an agv type has been acknowledged by the operator.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record RequestAgvTypeChangeAcknowledgement(Guid CorrelationId) : CorrelatedBy<Guid>;