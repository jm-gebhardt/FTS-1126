using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Command to mark the AgvGraph existing in the Agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record SetAgvGraphExist(Guid CorrelationId) : CorrelatedBy<Guid>;