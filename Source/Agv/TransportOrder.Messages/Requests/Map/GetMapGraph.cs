namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Provides a request to get a map graph.
/// </summary>
/// <param name="CorrelationId">The correlation id of the map.</param>
public record GetMapGraph(Guid CorrelationId);