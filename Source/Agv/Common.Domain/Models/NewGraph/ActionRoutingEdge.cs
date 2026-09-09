
// ReSharper disable UnusedType.Global

using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// An edge in the routing graph on which an action can be run.
/// </summary>
public record ActionRoutingEdge : RoutingEdge
{
    /// <summary>
    /// The action type.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public RoutingActionType ActionType { get; init; }

    /// <inheritdoc/>
    public ActionRoutingEdge(Guid id,
        Guid agvTypeId,
        Guid startNodeId,
        Guid endNodeId,
        double length,
        double? vehicleOrientation,
        OrientationType orientationType,
        double? rotationAtStart,
        double? rotationAtEnd,
        bool rotationAllowed,
        double? maxSpeed,
        double? maxRotationSpeed,
        Trajectory? trajectory,
        RoutingActionType actionType) 
        : base(id, agvTypeId, startNodeId, endNodeId, length, vehicleOrientation, orientationType, rotationAtStart, rotationAtEnd, rotationAllowed, maxSpeed, maxRotationSpeed, trajectory)
    {
        ActionType = actionType;
    }
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
