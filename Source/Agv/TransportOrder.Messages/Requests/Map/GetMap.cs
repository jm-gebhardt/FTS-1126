namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Provides a request to get information of a map.
/// </summary>
/// <param name="CorrelationId">The correlation id of the map.</param>
public record GetMap(Guid CorrelationId);