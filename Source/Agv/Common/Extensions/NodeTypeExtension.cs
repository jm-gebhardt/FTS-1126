using Gebhardt.Acc.Common.Messages.ActionGroups;
using static Gebhardt.Acc.Common.Domain.Models.Graphs.AgvGraph;

namespace Gebhardt.Acc.Common.Extensions;

/// <summary>
/// Provides extension methods for node types.
/// </summary>
public static class NodeTypeExtension
{
    /// <summary>
    /// Get the node type that matches the given action type.
    /// </summary>
    /// <param name="nodeTypes">The node types.</param>
    /// <param name="actionType">The action type.</param>
    /// <returns>The appropriate node type.</returns>
    public static NodeType GetTypeForActionType(this ICollection<NodeType> nodeTypes, ActionType actionType)
    {
        return actionType switch
        {
            ActionType.Pick => nodeTypes.FirstOrDefault(n => n is NodeType.Pick or NodeType.PickDrop),
            ActionType.Drop => nodeTypes.FirstOrDefault(n => n is NodeType.Drop or NodeType.PickDrop),
            ActionType.Charge => nodeTypes.FirstOrDefault(n => n == NodeType.Charge),
            ActionType.Move => nodeTypes.FirstOrDefault(),
            _ => NodeType.WayPoint
        };
    }

    /// <summary>
    /// Converts an action type into the node type that the action can be performed on.
    /// </summary>
    /// <param name="actionType">The action type to convert.</param>
    public static NodeType ToNodeType(this ActionType actionType)
    {
        return actionType switch
        {
            ActionType.Pick => NodeType.Pick,
            ActionType.Drop => NodeType.Drop,
            ActionType.Move => NodeType.WayPoint,
            ActionType.Charge => NodeType.Charge,
            _ => NodeType.WayPoint
        };
    }

    /// <summary>
    /// True, if the node types contain the given type.
    /// </summary>
    /// <param name="nodeTypes">The node types.</param>
    /// <param name="nodeType">The node type to compare to.</param>
    public static bool IsOfType(this ICollection<NodeType> nodeTypes, NodeType nodeType) =>
        nodeTypes.Contains(nodeType);
}