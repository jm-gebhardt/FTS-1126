using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.AgvType;

/// <summary>
/// Provides a request to get all agv types.
/// </summary>
/// <param name="CorrelationId">The correlation id of the request.</param>
public record GetAllAgvTypes(Guid CorrelationId) : CorrelatedBy<Guid>;
