using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

/// <summary>
/// An edge in the routing graph. Each routing edge connects two routing nodes and represents a possible transition between them.
/// </summary>
public class RoutingEdge : RoutingResource
{
    /// <summary>
    /// Start node of the edge.
    /// </summary>
    public RoutingNode StartNode { get; }

    /// <summary>
    /// End node of the edge. Can be the same as the start node.
    /// </summary>
    public RoutingNode EndNode { get; }

    /// <summary>
    /// Length of the edge in [m]
    /// </summary>
    public double Length { get; }

    /// <summary>
    /// Vehicle orientation in [rad] for this edge.  The value VehicleOrientation defines if it has to be interpreted. If null, there is no specific vehicle orientation defined for this edge.
    /// </summary>
    public double? VehicleOrientation { get; }

    /// <summary>
    /// Defines the type of orientation for this edge.
    /// </summary>
    public OrientationType OrientationType { get; }

    /// <summary>
    /// Rotation at the start of the edge in [rad]. If null, there is no specific rotation before traversing this edge.
    /// </summary>
    public double? RotationAtStart { get; }

    /// <summary>
    /// Rotation at the start of the edge in [rad]. If null, there is no specific rotation before traversing this edge.
    /// </summary>
    public double? RotationAtEnd { get; }

    /// <summary>
    /// Indicates whether rotation is allowed on this edge. If true, the AGV can rotate while traversing this edge.
    /// </summary>
    public bool RotationAllowed { get; }

    /// <summary>
    /// Maximum speed allowed on this edge in [m/s]. If null, there is no specific speed limit for this edge.
    /// </summary>
    public double? MaxSpeed { get; }

    /// <summary>
    /// Maximum rotation speed allowed on this edge in [rad/s]. If null, there is no specific rotation speed limit for this edge.
    /// </summary>
    public double? MaxRotationSpeed { get; }

    /// <summary>
    /// Optional trajectory associated with this edge. If null, the edge does not have a specific trajectory defined.
    /// </summary>
    public Trajectory? Trajectory { get; }
    
    public override string Description => $"{StartNode.Description} -> {EndNode.Description}";

    /// <inheritdoc cref="RoutingEdge"/>
    public RoutingEdge(
        Guid id,
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
        Trajectory? trajectory) 
        : base(id, parent, agvType)
    {
        StartNode = startNode;
        EndNode = endNode;
        Length = length;
        VehicleOrientation = vehicleOrientation;
        OrientationType = orientationType;
        RotationAtStart = rotationAtStart;
        RotationAtEnd = rotationAtEnd;
        RotationAllowed = rotationAllowed;
        MaxSpeed = maxSpeed;
        MaxRotationSpeed = maxRotationSpeed;
        Trajectory = trajectory;
    }

    public override string ToString()
    {
        return $"{nameof(RoutingEdge)} {StartNode} -> {EndNode}, Length={Length:f3}";
    }
}