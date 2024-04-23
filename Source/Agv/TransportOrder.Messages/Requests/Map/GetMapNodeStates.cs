namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Provides a request to get the node states of all nodes on a map.
/// </summary>
/// <param name="CorrelationId">The map correlation id.</param>
public record GetMapNodeStates(Guid CorrelationId);