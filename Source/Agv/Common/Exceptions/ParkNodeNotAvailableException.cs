namespace Gebhardt.Acc.Common.Exceptions;

/// <summary>
/// Park node is not available.
/// </summary>
public class ParkNodeNotAvailableException(Guid nodeId) : NodeNotAvailableException(nodeId.ToString());