namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Provides an exception raise when a node has not been defined in the agv graph.
/// </summary>
public class NodeNotDefinedForAgvException: Exception
{
    /// <inheritdoc/>
    public NodeNotDefinedForAgvException(Guid nodeId, Guid agvCorrelationId) : base($"Action node {nodeId} not defined in graph of agv {agvCorrelationId}"){}
}