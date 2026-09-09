using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// An edge in the routing graph on which an action can be run.
/// </summary>
[UsedImplicitly]
public class ActionRoutingEdge : RoutingEdge
{
    /// <inheritdoc/>
    public ActionRoutingEdge(Guid id, GraphResource parent, AgvType agvType, RoutingNode startNode, RoutingNode endNode, double length, bool rotationAllowed, double? maxSpeed, double? maxRotationSpeed, Trajectory? trajectory, RoutingActionType actionType) : base(id, parent, agvType, startNode, endNode, length, rotationAllowed, maxSpeed, maxRotationSpeed, trajectory)
    {
        ActionType = actionType;
    }

    /// <summary>
    /// Efcore
    /// </summary>
    public ActionRoutingEdge()
    {
    }

    /// <summary>
    /// The action type.
    /// </summary>
    [UsedImplicitly]
    public RoutingActionType ActionType { get; }
}

/// <summary>
/// The type of the action.
/// </summary>
[PublicAPI]
public enum RoutingActionType
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    Pick,
    Drop,
    Rotate
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
