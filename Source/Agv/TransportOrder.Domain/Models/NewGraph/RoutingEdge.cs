using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// An edge in the routing graph. Each routing edge connects two routing nodes and represents a possible transition between them.
/// </summary>
[UsedImplicitly]
public class RoutingEdge : RoutingResource
{
    /// <summary>
    /// Start node of the edge.
    /// </summary>
    [UsedImplicitly]
    public RoutingNode StartNode { get; }

    /// <summary>
    /// End node of the edge. Can be the same as the start node.
    /// </summary>
    [UsedImplicitly]
    public RoutingNode EndNode { get; }

    /// <summary>
    /// Length of the edge in [m]
    /// </summary>
    [UsedImplicitly]
    public double Length { get; }  //does length exist without trajectory?, decide which length is used for rotations

    //public double DrivingTime { get; } // create method to calculate driving time based on length and max speed, maybe also consider acceleration and deceleration

    /// <summary>
    /// Indicates whether rotation is allowed on this edge. If true, the AGV can rotate while traversing this edge.
    /// </summary>
    [UsedImplicitly]
    public bool RotationAllowed { get; }

    /// <summary>
    /// Maximum speed allowed on this edge in [m/s]. If null, there is no specific speed limit for this edge.
    /// </summary>
    [UsedImplicitly]
    public double? MaxSpeed { get; }

    /// <summary>
    /// Maximum rotation speed allowed on this edge in [rad/s]. If null, there is no specific rotation speed limit for this edge.
    /// </summary>
    [UsedImplicitly]
    public double? MaxRotationSpeed { get; }

    /// <summary>
    /// Optional trajectory associated with this edge. If null, the edge does not have a specific trajectory defined.
    /// </summary>
    [UsedImplicitly]
    public Trajectory? Trajectory { get; }

    /// <inheritdoc cref="RoutingEdge"/>
    public RoutingEdge(Guid id, GraphResource parent, AgvType agvType, RoutingNode startNode, RoutingNode endNode, double length, bool rotationAllowed, double? maxSpeed, double? maxRotationSpeed, Trajectory? trajectory) 
        : base(id, parent, agvType)
    {
        StartNode = startNode;
        EndNode = endNode;
        Length = length;
        RotationAllowed = rotationAllowed;
        MaxSpeed = maxSpeed;
        MaxRotationSpeed = maxRotationSpeed;
        Trajectory = trajectory;
    }
}