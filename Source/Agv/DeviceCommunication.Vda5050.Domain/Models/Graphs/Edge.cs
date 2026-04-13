using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;

/// <summary>
/// An edge and agv can access.
/// </summary>
[PublicAPI]
public record Edge
{
    /// <summary>
    /// Edge identification used by this agv fleet .
    /// </summary>
    public string EdgeId { get; set; } = string.Empty;

    /// <summary>
    /// Unique id of the edee across fleets
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The node id of the start node.
    /// </summary>
    public string StartNodeId { get; set; } = string.Empty;

    /// <summary>
    /// The node id of the end node.
    /// </summary>
    public string EndNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Get agv actions.
    /// </summary>
    public ICollection<AgvEdgeAction> AgvActions { get; set; } = [];

    /// <summary>
    /// The trajectory the agv drives when running on this edge.
    /// </summary>
    public Trajectory AgvTrajectory { get; set; } = null!;

    /// <summary>
    /// The agv orientation [rad].
    /// </summary>
    public double? AgvOrientation { get; set; }

    /// <summary>
    /// The orientation type.
    /// </summary>
    public OrientationType OrientationType { get; set; }

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
    /// The time the node was created.
    /// </summary>
    public DateTime Timestamp { get; init; }
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