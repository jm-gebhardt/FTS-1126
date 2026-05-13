using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable InconsistentNaming

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;

/// <summary>
/// Order message from master control to the AGV.
/// <para>See "order.schema".</para>
/// </summary>
[PublicAPI]
[EntityName("amq.topic")] // Specifies the RabbitMq exchange where the message is published
[RoutingKey("*.*.*.*.order")] // Specifies the topic where subscribers can receive the message
public record MqttOrder
{
    /// <summary>
    /// HeaderId of the message.
    /// The headerId is defined per topic and incremented by 1 with each sent (but not necessarily received) message.
    /// </summary>
    public uint HeaderId { get; set; }

    /// <summary>
    /// Timestamp in ISO8601 format (YYYY-MM-DDTHH:mm:ss.ffZ).
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Version of the protocol [Major].[Minor].[Patch].
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Manufacturer of the AGV.
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Serial number of the AGV.
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Order identification. Used to identify multiple order messages that belong to the same order.
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Order update identification. Is unique per orderId.
    /// If an order update is rejected, this field is to be passed in the rejection message.
    /// </summary>
    public int OrderUpdateId { get; set; }

    /// <summary>
    /// Unique identifier of the zone set that the AGV has to use for navigation or that was used by MC for planning.
    /// Optional: Do not add if no zones are used.
    /// </summary>
    public string? ZoneSetId { get; set; }

    /// <summary>
    /// Array of nodes to be traversed for fulfilling the order.
    /// One node is enough for a valid order; leave edge list empty for that case.
    /// </summary>
    public ICollection<MqttOrderNode> Nodes { get; set; } = [];

    /// <summary>
    /// Array of edges to be traversed for fulfilling the order.
    /// </summary>
    public ICollection<MqttOrderEdge> Edges { get; set; } = [];
}

/// <summary>
/// Node to be traversed for fulfilling an order.
/// </summary>
[PublicAPI]
public record MqttOrderNode
{
    /// <summary>
    /// Unique node identification.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Number to track the sequence of nodes and edges in an order and to simplify order updates.
    /// Runs across all nodes and edges of the same order and is reset when a new orderId is issued.
    /// </summary>
    public int SequenceId { get; set; }

    /// <summary>
    /// Additional information on the node.
    /// </summary>
    public string? NodeDescription { get; set; }

    /// <summary>
    /// True indicates that the node is part of the base.
    /// False indicates that the node is part of the horizon.
    /// </summary>
    public bool Released { get; set; }

    /// <summary>
    /// Position of the node on a map in world coordinates.
    /// Optional for vehicle types that do not require the node position (e.g., line-guided vehicles).
    /// </summary>
    public MqttOrderNodePosition? NodePosition { get; set; }

    /// <summary>
    /// Array of actions to be executed on this node. Empty array if no actions required.
    /// </summary>
    public ICollection<MqttOrderAction> Actions { get; set; } = [];
}

/// <summary>
/// Position of a node on a map in world coordinates, as specified in an order.
/// </summary>
[PublicAPI]
public record MqttOrderNodePosition
{
    /// <summary>X-position on the map in reference to the map coordinate system.</summary>
    public double X { get; set; }

    /// <summary>Y-position on the map in reference to the map coordinate system.</summary>
    public double Y { get; set; }

    /// <summary>
    /// Absolute orientation of the AGV on the node in radians [-π, π].
    /// Optional: vehicle can plan the path by itself.
    /// </summary>
    public double? Theta { get; set; }

    /// <summary>
    /// Allowed deviation radius in meters for the AGV to count the node as traversed.
    /// 0 means no deviation is allowed beyond normal AGV manufacturer tolerance.
    /// Optional.
    /// </summary>
    public double? AllowedDeviationXy { get; set; }

    /// <summary>
    /// Maximum allowed deviation of the theta angle in radians [0, π].
    /// Optional.
    /// </summary>
    public double? AllowedDeviationTheta { get; set; }

    /// <summary>
    /// Unique identification of the map in which the position is referenced.
    /// </summary>
    public string MapId { get; set; } = string.Empty;

    /// <summary>
    /// Additional information on the map.
    /// </summary>
    public string? MapDescription { get; set; }
}

/// <summary>
/// Directional connection between two nodes to be traversed for fulfilling an order.
/// </summary>
[PublicAPI]
public record MqttOrderEdge
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
    /// If not defined, the default value is <see cref="MqttOrientationType.TANGENTIAL"/>.
    /// </summary>
    public MqttOrientationType? OrientationType { get; set; }

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
    public MqttTrajectory? Trajectory { get; set; }

    /// <summary>
    /// Boundaries within which the vehicle can deviate from its trajectory, e.g., to avoid obstacles.
    /// Optional.
    /// </summary>
    public MqttCorridor? Corridor { get; set; }

    /// <summary>
    /// Array of actions to be executed on this edge.
    /// </summary>
    public ICollection<MqttOrderAction> Actions { get; set; } = [];
}

/// <summary>
/// Definition of boundaries in which a vehicle can deviate from its trajectory.
/// </summary>
[PublicAPI]
public record MqttCorridor
{
    /// <summary>
    /// Width of the corridor in meters to the left of the vehicle's trajectory.
    /// </summary>
    public double LeftWidth { get; set; }

    /// <summary>
    /// Width of the corridor in meters to the right of the vehicle's trajectory.
    /// </summary>
    public double RightWidth { get; set; }

    /// <summary>
    /// Defines whether the boundaries are valid for the kinematic center or the contour of the vehicle.
    /// Optional.
    /// </summary>
    public MqttCorridorRefPoint? CorridorRefPoint { get; set; }
}

/// <summary>
/// Reference point for corridor boundary calculations.
/// </summary>
[PublicAPI]
public enum MqttCorridorRefPoint
{
    /// <summary>Boundaries are valid for the kinematic center of the vehicle.</summary>
    KINEMATICCENTER,

    /// <summary>Boundaries are valid for the contour of the vehicle.</summary>
    CONTOUR
}

/// <summary>
/// Orientation type for an edge.
/// </summary>
[PublicAPI]
public enum MqttOrientationType
{
    /// <summary>Orientation is relative to the global project-specific map coordinate system.</summary>
    GLOBAL,

    /// <summary>Orientation is tangential to the edge. 0.0 = forwards, π = backwards.</summary>
    TANGENTIAL
}

/// <summary>
/// Action that the AGV is to perform on a node or edge.
/// </summary>
[PublicAPI]
public record MqttOrderAction
{
    /// <summary>
    /// Name of the action as described in "Actions and Parameters". Identifies the function of the action.
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Unique ID to identify the action and map it to the actionState in the state.
    /// </summary>
    public string ActionId { get; set; } = string.Empty;

    /// <summary>
    /// Additional information on the action.
    /// </summary>
    public string? ActionDescription { get; set; }

    /// <summary>
    /// Regulates if the action is allowed to be executed during movement and/or parallel to other actions.
    /// </summary>
    public BlockingType BlockingType { get; set; }

    /// <summary>
    /// Array of action parameters for the indicated action.
    /// Optional: omit if the action has no parameters.
    /// </summary>
    public ICollection<MqttActionParameter>? ActionParameters { get; set; }
}

