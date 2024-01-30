using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;

/// <summary>
/// Provides a dto for node states.
/// </summary>
/// <param name="NodeId">The node id.</param>
/// <param name="NodeState">The node state.</param>
/// <param name="IsLocked">If true, node is locked.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv the node is reserved for/ occupied by.</param>
public record NodeStateDto(string NodeId, NodeLockState NodeState, bool IsLocked, Guid? AgvCorrelationId) : DatedMessage;