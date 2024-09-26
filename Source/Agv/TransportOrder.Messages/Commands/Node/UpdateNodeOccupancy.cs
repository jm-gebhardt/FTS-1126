namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Node;

/// <summary>
/// Provides a command to update the node occupancy of an agv.
/// </summary>
/// <param name="AgvCorrelationId">The agv correlation id.</param>
/// <param name="OldLastNodeId">The old last node id of the agv.</param>
/// <param name="NewLastNodeId">The new last node id of the agv.</param>
public record UpdateNodeOccupancy(Guid AgvCorrelationId, string? OldLastNodeId, string? NewLastNodeId);