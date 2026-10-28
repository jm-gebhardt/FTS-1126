using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

/// <summary>
/// An edge in the routing graph on which an action can be run.
/// </summary>
public class ActionRoutingEdge : RoutingEdge
{
    /// <inheritdoc/>
    public ActionRoutingEdge(Guid id,
        GraphResource parent,
        AgvType agvType,
        RoutingNode startNode,
        RoutingNode endNode,
        double length,
        double? vehicleOrientation,
        OrientationType orientationType,
        double? rotationAtStart,
        double? rotationAtEnd,
        bool rotationAllowed,
        double? maxSpeed,
        double? maxRotationSpeed,
        Trajectory? trajectory,
        RoutingActionType actionType) : base(id,
        parent,
        agvType,
        startNode,
        endNode,
        length,
        vehicleOrientation,
        orientationType,
        rotationAtStart,
        rotationAtEnd,
        rotationAllowed,
        maxSpeed,
        maxRotationSpeed,
        trajectory)
    {
        ActionType = actionType;
    }

    public RoutingActionType ActionType { get; }
}
