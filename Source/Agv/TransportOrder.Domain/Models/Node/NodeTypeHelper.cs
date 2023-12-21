using Gebhardt.StoreWare.Agv.Common.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;

/// <summary>
/// Helper methods for dealing with node types
/// </summary>
public static class NodeTypeHelper
{
    /// <summary>
    /// Determines whether the NodeType is a action node type
    /// </summary>
    /// <param name="nodeType">The node type to check</param>
    /// <returns>True if the mode type is Pick, Drop or PickDrop</returns>
    public static bool IsActionNodeType(AgvGraph.NodeType nodeType)
    {
        return nodeType is AgvGraph.NodeType.Pick or
            AgvGraph.NodeType.Drop or
            AgvGraph.NodeType.PickDrop;
    }

    /// <summary>
    /// Determines whether the Node is of type source
    /// </summary>
    /// <param name="nodeType">The node type to check</param>
    /// <returns>True if it is a Pick or PickDrop</returns>
    public static bool IsSourceNodeType(AgvGraph.NodeType nodeType)
    {
        return nodeType is AgvGraph.NodeType.Pick or
            AgvGraph.NodeType.PickDrop;
    }

    /// <summary>
    /// Determines whether the Node is of type destination
    /// </summary>
    /// <param name="nodeType">The node type to check</param>
    /// <returns>True if it is a Drop or PickDrop</returns>
    public static bool IsDestinationNodeType(AgvGraph.NodeType nodeType)
    {
        return nodeType is AgvGraph.NodeType.Drop or
            AgvGraph.NodeType.PickDrop;
    }
}