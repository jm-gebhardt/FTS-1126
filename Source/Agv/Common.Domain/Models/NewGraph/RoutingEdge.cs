
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// An edge in the routing graph. Each routing edge connects two routing nodes and represents a possible transition between them.
/// </summary>
[PublicAPI]
[JsonPolymorphic]
[JsonDerivedType(typeof(RoutingEdge), nameof(RoutingEdge))]
[JsonDerivedType(typeof(ActionRoutingEdge), nameof(ActionRoutingEdge))]
public record RoutingEdge : RoutingResource
{
    /// <summary>
    /// Id of the start node of the edge.
    /// </summary>
    public Guid StartNodeId { get; init; }

    /// <summary>
    /// Id of the end node of the edge. Can be the same as the start node.
    /// </summary>
    public Guid EndNodeId { get; init; }

    /// <summary>
    /// Length of the edge in [m]
    /// </summary>
    public double Length { get; init; }  //does length exist without trajectory?, decide which length is used for rotations

    /// <summary>
    /// Vehicle orientation in [rad] for this edge.  The value VehicleOrientation defines if it has to be interpreted. If null, there is no specific vehicle orientation defined for this edge.
    /// </summary>
    public double? VehicleOrientation { get; init; }

    /// <summary>
    /// Defines the type of orientation for this edge.
    /// </summary>
    public OrientationType OrientationType { get; init; }

    /// <summary>
    /// Rotation at the start of the edge in [rad]. If null, there is no specific rotation before traversing this edge.
    /// </summary>
    public double? RotationAtStart { get; init; }

    /// <summary>
    /// Rotation at the end of the edge in [rad]. If null, there is no specific rotation before traversing this edge.
    /// </summary>
    public double? RotationAtEnd { get; init; }

    /// <summary>
    /// Indicates whether rotation is allowed on this edge. If true, the AGV can rotate while traversing this edge.
    /// </summary>
    public bool RotationAllowed { get; init; }

    /// <summary>
    /// Maximum speed allowed on this edge in [m/s]. If null, there is no specific speed limit for this edge.
    /// </summary>
    public double? MaxSpeed { get; init; }

    /// <summary>
    /// Maximum rotation speed allowed on this edge in [rad/s]. If null, there is no specific rotation speed limit for this edge.
    /// </summary>
    public double? MaxRotationSpeed { get; init; }

    /// <summary>
    /// Optional trajectory associated with this edge. If null, the edge does not have a specific trajectory defined.
    /// </summary>
    public Trajectory? Trajectory { get; init; }

    /// <inheritdoc cref="RoutingEdge"/>
    public RoutingEdge(
        Guid id,
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
        Trajectory? trajectory)
        : base(id, agvTypeId)
    {
        StartNodeId = startNodeId;
        EndNodeId = endNodeId;
        Length = length;
        RotationAllowed = rotationAllowed;
        MaxSpeed = maxSpeed;
        MaxRotationSpeed = maxRotationSpeed;
        Trajectory = trajectory;
        RotationAtStart = rotationAtStart;
        RotationAtEnd = rotationAtEnd;
        VehicleOrientation = vehicleOrientation;
        OrientationType = orientationType;
    }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    public RoutingEdge()
    {
    }
}

/// <summary>
/// Defines the type of orientation for a routing edge.
/// </summary>
public enum OrientationType
{
    /// <summary>
    /// The vehicle orientation is tangential to the trajectory of the edge. This is the default value.
    /// </summary>
    Tangential,

    /// <summary>
    /// The vehicle orientation on the edge is defined in the global coordinate system.
    /// </summary>
    Global
}