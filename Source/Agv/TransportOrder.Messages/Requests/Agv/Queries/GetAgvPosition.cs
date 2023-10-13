namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Provides a request for the position of an agv.
/// </summary>
public record GetAgvPosition(Guid CorrelationId);