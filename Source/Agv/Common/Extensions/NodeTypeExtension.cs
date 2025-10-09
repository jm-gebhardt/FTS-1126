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
    /// <param name="nodeType">The node type object.</param>
    /// <param name="actionType">The action type.</param>
    /// <returns>The appropriate node type.</returns>
    ///<exception cref="InvalidCastException">If object is not of type <see cref="NodeType"/> or a collection of it.</exception>
    public static NodeType GetTypeForActionType(this object nodeType, ActionType actionType)
    {
        if (nodeType is NodeType type)
        {
            return type;
        }
        var availableNodes = (ICollection<NodeType>)nodeType;
        return actionType switch
        {
            ActionType.Pick => availableNodes.FirstOrDefault(n => n is NodeType.Pick or NodeType.PickDrop),
            ActionType.Drop => availableNodes.FirstOrDefault(n => n is NodeType.Drop or NodeType.PickDrop),
            ActionType.Charge => availableNodes.FirstOrDefault(n => n == NodeType.Charge),
            ActionType.Move => availableNodes.FirstOrDefault(),
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
    /// True, if the node is of the given type.
    /// </summary>
    /// <param name="type">The node type.</param>
    /// <param name="nodeType">The node type to compare to.</param>
    public static bool IsOfType(this object type, NodeType nodeType)
    {
        if (type is NodeType singleType)
        {
            return singleType == nodeType;
        }

        if (type is ICollection<NodeType> multipleTypes)
        {
            return multipleTypes.Contains(nodeType);
        }
        return false;
    }
}