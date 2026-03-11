namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Represents the exception raised when the count of edges does not match the count of nodes.
/// </summary>
public class OrderNodeAndEdgeMismatchException: Exception
{
    /// <inheritdoc/>
    public OrderNodeAndEdgeMismatchException(int edgeCount, int nodeCount, Guid orderId, Guid agvCorrelationId) : base($"Mismatch of edge {edgeCount} and node {nodeCount} count for order {orderId} of agv {agvCorrelationId}, expect: edges = nodes - 1"){}
}