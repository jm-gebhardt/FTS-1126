namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Park node is not available.
/// </summary>
public class ParkNodeNotAvailableException(string nodeId) : NodeNotAvailableException($"Park node {nodeId} is not available");