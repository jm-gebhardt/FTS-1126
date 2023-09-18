using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// FreeNodeFailedException
/// </summary>
public class NodeStateException : Exception
{
    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="nodeId">The nodeId of the node</param>
    /// <param name="action">The action you tried to do, e.g. lock</param>
    [UsedImplicitly]
    public NodeStateException(string nodeId, string action) : base($"Unable to {action} node with Id: {nodeId}")
    {
    }
}