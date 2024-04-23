using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Command to mark the AgvGraph existing in the Agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record SetAgvGraphExist(Guid CorrelationId) : CorrelatedBy<Guid>;