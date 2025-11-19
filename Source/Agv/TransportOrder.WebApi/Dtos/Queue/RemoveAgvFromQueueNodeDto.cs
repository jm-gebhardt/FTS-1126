namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Queue;

/// <summary>
/// Remove an AGV from a queue node.
/// </summary>
/// <param name="AgvCorrelationId">The Agv CorrelationId you want to clear from that node.</param>
public record RemoveAgvFromQueueNodeDto(Guid AgvCorrelationId);