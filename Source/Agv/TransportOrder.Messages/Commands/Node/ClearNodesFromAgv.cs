namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Node;

/// <summary>
/// Provides a command to clear all nodes from the given agv.
/// </summary>
/// <param name="AgvCorrelationId">The agv correlation id.</param>
public record ClearNodesFromAgv(Guid AgvCorrelationId);