namespace Gebhardt.Acc.Common.Exceptions;

/// <summary>
/// Node is not available.
/// </summary>
/// <param name="nodeId">The node Id which is not available.</param>
public class NodeNotAvailableException(string nodeId) : Exception($"Node: {nodeId} is not available");