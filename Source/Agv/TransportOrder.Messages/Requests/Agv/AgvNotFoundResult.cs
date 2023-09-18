using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Defines a result for the case that an agv for a request was not found
/// </summary>
public record AgvNotFoundResult(Guid CorrelationId) : CorrelatedBy<Guid>;