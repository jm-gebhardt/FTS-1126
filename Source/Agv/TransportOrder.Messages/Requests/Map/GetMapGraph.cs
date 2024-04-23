namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Provides a request to get a graph for a specific map.
/// </summary>
/// <param name="CorrelationId">The correlation id of the map.</param>
public record GetMapGraph(Guid CorrelationId);