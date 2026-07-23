using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

/// <summary>
/// Directional connection between two nodes to be traversed for fulfilling an order.
/// </summary>
[PublicAPI]
public class OrderEdge
{
    /// <summary>
    /// Unique edge identification.
    /// </summary>
    public string EdgeId { get; set; } = string.Empty;

    /// <summary>
    /// Number to track the sequence of nodes and edges in an order and to simplify order updates.
    /// Runs across all nodes and edges of the same order and is reset when a new orderId is issued.
    /// </summary>
    public int SequenceId { get; set; }

    /// <summary>
    /// Additional information on the edge.
    /// </summary>
    public string? EdgeDescription { get; set; }

    /// <summary>
    /// True indicates that the edge is part of the base.
    /// False indicates that the edge is part of the horizon.
    /// </summary>
    public bool Released { get; set; }

    /// <summary>
    /// The nodeId of the start node.
    /// </summary>
    public string StartNodeId { get; set; } = string.Empty;

    /// <summary>
    /// The nodeId of the end node.
    /// </summary>
    public string EndNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Permitted maximum speed on the edge in m/s. Optional: no limit if not set.
    /// </summary>
    public double? MaxSpeed { get; set; }

    /// <summary>
    /// Permitted maximum height of the vehicle including the load on the edge in meters.
    /// Optional: no limit if not set.
    /// </summary>
    public double? MaxHeight { get; set; }

    /// <summary>
    /// Permitted minimum height of the load handling device on the edge in meters.
    /// Optional: no limit if not set.
    /// </summary>
    public double? MinHeight { get; set; }

    /// <summary>
    /// Orientation of the AGV on the edge in radians [-π, π].
    /// Optional.
    /// </summary>
    public double? Orientation { get; set; }

    /// <summary>
    /// Defines whether orientation is interpreted relative to the global map coordinate system or tangential to the edge.
    /// If not defined, the default value is <see cref="OrientationType.Tangential"/>.
    /// </summary>
    public OrientationType? OrientationType { get; set; }

    /// <summary>
    /// Sets direction at junctions for line-guided or wire-guided vehicles.
    /// Optional.
    /// </summary>
    public string? Direction { get; set; }

    /// <summary>
    /// True: rotation is allowed on the edge. False: rotation is not allowed on the edge.
    /// Optional: no limit if not set.
    /// </summary>
    public bool? RotationAllowed { get; set; }

    /// <summary>
    /// Maximum rotation speed in rad/s. Optional: no limit if not set.
    /// </summary>
    public double? MaxRotationSpeed { get; set; }

    /// <summary>
    /// Distance of the path from startNode to endNode in meters.
    /// Optional: used by line-guided AGVs to decrease speed before reaching a stop position.
    /// </summary>
    public double? Length { get; set; }

    /// <summary>
    /// Trajectory as a NURBS for this edge.
    /// Optional: can be omitted if the AGV plans its own trajectory.
    /// </summary>
    public Trajectory? Trajectory { get; set; }

    /// <summary>
    /// Boundaries within which the vehicle can deviate from its trajectory, e.g., to avoid obstacles.
    /// Optional.
    /// </summary>
    public Corridor? Corridor { get; set; }

    /// <summary>
    /// Array of actions to be executed on this edge.
    /// </summary>
    public ICollection<OrderAction> Actions { get; set; } = [];
}

/// <summary>
/// Orientation type for an edge.
/// </summary>
[PublicAPI]
public enum OrientationType
{
    /// <summary>Orientation is relative to the global project-specific map coordinate system.</summary>
    Global,

    /// <summary>Orientation is tangential to the edge. 0.0 = forwards, π = backwards.</summary>
    Tangential
}