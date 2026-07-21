using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;

/// <summary>
/// VDA5050 state of a simulated agv.
/// </summary>
[PublicAPI]
public record Vda5050StateDto : IMessageCreatedDate
{
    /// <summary>
    /// Correlation id of the agv this state belongs to.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <inheritdoc/>
    public DateTime MessageCreatedDate { get; init; }

    /// <summary>
    /// HeaderId of the message.
    /// </summary>
    public uint HeaderId { get; init; }

    /// <summary>
    /// Timestamp in ISO8601 format (YYYY-MM-DDTHH:mm:ss.ffZ).
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Version of the protocol [Major].[Minor].[Patch].
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Manufacturer of the AGV.
    /// </summary>
    public string Manufacturer { get; init; } = string.Empty;

    /// <summary>
    /// Serial number of the AGV.
    /// </summary>
    public string SerialNumber { get; init; } = string.Empty;

    /// <summary>
    /// Array of map-objects that are currently stored on the vehicle.
    /// </summary>
    public ICollection<MapDto>? Maps { get; init; }

    /// <summary>
    /// Unique order identification of the current order or the previous finished order.
    /// </summary>
    public string OrderId { get; init; } = string.Empty;

    /// <summary>
    /// Order Update Identification to identify that an order update has been accepted by the AGV.
    /// </summary>
    public int OrderUpdateId { get; init; }

    /// <summary>
    /// Unique ID of the zone set that the AGV currently uses for path planning.
    /// </summary>
    public string? ZoneSetId { get; init; }

    /// <summary>
    /// NodeID of last reached node or, if AGV is currently on a node, current node.
    /// </summary>
    public string? LastNodeId { get; init; }

    /// <summary>
    /// SequenceId of the last reached node or, if the AGV is currently on a node, sequenceId of current node.
    /// </summary>
    public int LastNodeSequenceId { get; init; }

    /// <summary>
    /// True: indicates that the AGV is driving and/or rotating.
    /// False: indicates that the AGV is neither driving nor rotating.
    /// </summary>
    public bool Driving { get; init; }

    /// <summary>
    /// True: AGV is currently in a paused state. False: The AGV is currently not in a paused state.
    /// </summary>
    public bool? Paused { get; init; }

    /// <summary>
    /// True: AGV is almost at the end of the base and will reduce speed if no new base is transmitted.
    /// False: no base update required.
    /// </summary>
    public bool? NewBaseRequest { get; init; }

    /// <summary>
    /// Used by line guided vehicles to indicate the distance it has been driving past the lastNodeId in meters.
    /// </summary>
    public float? DistanceSinceLastNode { get; init; }

    /// <summary>
    /// Current operating mode of the AGV.
    /// </summary>
    public OperatingModeDto OperatingMode { get; init; }

    /// <summary>
    /// Array of nodeState-objects that need to be traversed for fulfilling the order. Empty list if idle.
    /// </summary>
    public IList<NodeStateDto> NodeStates { get; init; } = [];

    /// <summary>
    /// Array of edgeState-objects that need to be traversed for fulfilling the order. Empty list if idle.
    /// </summary>
    public IList<EdgeStateDto> EdgeStates { get; init; } = [];

    /// <summary>
    /// Current position of the AGV on the map.
    /// </summary>
    public Vda5050AgvPositionDto? AgvPosition { get; init; }

    /// <summary>
    /// The AGV's velocity in vehicle coordinates.
    /// </summary>
    public VelocityDto? Velocity { get; init; }

    /// <summary>
    /// Loads that are currently handled by the AGV.
    /// </summary>
    public IList<LoadDto>? Loads { get; init; }

    /// <summary>
    /// Contains a list of the current actions and the actions which are yet to be finished.
    /// </summary>
    public IList<ActionStateDto> ActionStates { get; init; } = [];

    /// <summary>
    /// Contains all battery-related information.
    /// </summary>
    public BatteryStateDto BatteryState { get; init; } = new();

    /// <summary>
    /// Array of error-objects. All active errors of the AGV should be in the list.
    /// </summary>
    public IList<ErrorDto> Errors { get; init; } = [];

    /// <summary>
    /// Array of info-objects. This should only be used for visualization or debugging.
    /// </summary>
    public IList<InformationDto>? Information { get; init; }

    /// <summary>
    /// Contains all safety-related information.
    /// </summary>
    public SafetyStateDto SafetyState { get; init; } = new();
}

/// <summary>
/// Current operating mode of the AGV.
/// </summary>
[PublicAPI]
public enum OperatingModeDto
{
    /// <summary/>
    Automatic,

    /// <summary/>
    Manual,

    /// <summary/>
    Semiautomatic,

    /// <summary/>
    Service,

    /// <summary/>
    Teachin
}

/// <summary>
/// Map-object that is currently stored on the vehicle.
/// </summary>
[PublicAPI]
public record MapDto
{
    /// <summary>
    /// ID of the map describing a defined area of the vehicle's workspace.
    /// </summary>
    public string MapId { get; init; } = string.Empty;

    /// <summary>
    /// Version of the map.
    /// </summary>
    public string MapVersion { get; init; } = string.Empty;

    /// <summary>
    /// Additional information on the map.
    /// </summary>
    public string? MapDescription { get; init; }

    /// <summary>
    /// Information on the status of the map indicating, if a map version is currently used on the vehicle.
    /// </summary>
    public MapStatusDto MapStatus { get; init; }
}

/// <summary>
/// Information on the status of the map.
/// </summary>
[PublicAPI]
public enum MapStatusDto
{
    /// <summary>
    /// Indicates this map is currently active / used on the AGV.
    /// </summary>
    Enabled,

    /// <summary>
    /// Indicates this map version is currently not enabled on the AGV.
    /// </summary>
    Disabled
}

/// <summary>
/// State of a node that needs to be traversed for fulfilling the order.
/// </summary>
[PublicAPI]
public record NodeStateDto
{
    /// <summary>
    /// Unique node identification.
    /// </summary>
    public string NodeId { get; init; } = string.Empty;

    /// <summary>
    /// SequenceId to discern multiple nodes with same nodeId.
    /// </summary>
    public int SequenceId { get; init; }

    /// <summary>
    /// Additional information on the node.
    /// </summary>
    public string? NodeDescription { get; init; }

    /// <summary>
    /// True: indicates that the node is part of the base.
    /// False: indicates that the node is part of the horizon.
    /// </summary>
    public bool Released { get; init; }

    /// <summary>
    /// Node position. Optional: master control has this information.
    /// </summary>
    public NodePositionDto? NodePosition { get; init; }
}

/// <summary>
/// Position of a node on the map.
/// </summary>
[PublicAPI]
public record NodePositionDto
{
    /// <summary>X coordinate in meters.</summary>
    public double X { get; init; }

    /// <summary>Y coordinate in meters.</summary>
    public double Y { get; init; }

    /// <summary>Orientation of the AGV in radians.</summary>
    public double? Theta { get; init; }

    /// <summary>Unique identifier of the map the position refers to.</summary>
    public string MapId { get; init; } = string.Empty;
}

/// <summary>
/// State of an edge that needs to be traversed for fulfilling the order.
/// </summary>
[PublicAPI]
public record EdgeStateDto
{
    /// <summary>
    /// Unique edge identification.
    /// </summary>
    public string EdgeId { get; init; } = string.Empty;

    /// <summary>
    /// SequenceId of the edge.
    /// </summary>
    public int SequenceId { get; init; }

    /// <summary>
    /// Additional information on the edge.
    /// </summary>
    public string? EdgeDescription { get; init; }

    /// <summary>
    /// True indicates that the edge is part of the base.
    /// False indicates that the edge is part of the horizon.
    /// </summary>
    public bool Released { get; init; }

    /// <summary>
    /// The trajectory to be communicated as a NURBS.
    /// </summary>
    public TrajectoryDto? Trajectory { get; init; }
}

/// <summary>
/// Trajectory described as a NURBS.
/// </summary>
[PublicAPI]
public record TrajectoryDto
{
    /// <summary>
    /// Defines the number of control points that influence any given point on the curve.
    /// </summary>
    public int Degree { get; init; }

    /// <summary>
    /// Sequence of parameter values that determine where and how the control points affect the NURBS curve.
    /// </summary>
    public IList<float> KnotVector { get; init; } = [];

    /// <summary>
    /// List of control points of the NURBS, which includes the beginning and end point.
    /// </summary>
    public IList<ControlPointDto> ControlPoints { get; init; } = [];
}

/// <summary>
/// Control point of a NURBS trajectory.
/// </summary>
[PublicAPI]
public record ControlPointDto
{
    /// <summary>X coordinate in meters.</summary>
    public float X { get; init; }

    /// <summary>Y coordinate in meters.</summary>
    public float Y { get; init; }

    /// <summary>
    /// The weight with which this control point pulls on the curve.
    /// </summary>
    public float? Weight { get; init; }
}

/// <summary>
/// Load that is currently handled by the AGV.
/// </summary>
[PublicAPI]
public record LoadDto
{
    /// <summary>
    /// Unique identification number of the load (e.g., barcode or RFID).
    /// </summary>
    public string? LoadId { get; init; }

    /// <summary>Type of load.</summary>
    public string? LoadType { get; init; }

    /// <summary>
    /// Indicates which load handling/carrying unit of the AGV is used.
    /// </summary>
    public string? LoadPosition { get; init; }

    /// <summary>
    /// Point of reference for the location of the bounding box.
    /// </summary>
    public BoundingBoxReferenceDto? BoundingBoxReference { get; init; }

    /// <summary>
    /// Dimensions of the load's bounding box in meters.
    /// </summary>
    public LoadDimensionsDto? LoadDimensions { get; init; }

    /// <summary>
    /// Absolute weight of the load measured in kg.
    /// </summary>
    public double? Weight { get; init; }
}

/// <summary>
/// Point of reference for the location of a bounding box in AGV coordinate system.
/// </summary>
[PublicAPI]
public record BoundingBoxReferenceDto
{
    /// <summary>X coordinate in meters.</summary>
    public double X { get; init; }

    /// <summary>Y coordinate in meters.</summary>
    public double Y { get; init; }

    /// <summary>Z coordinate in meters.</summary>
    public double Z { get; init; }

    /// <summary>
    /// Orientation of the load's bounding box. Important for tugger trains, etc.
    /// </summary>
    public double? Theta { get; init; }
}

/// <summary>
/// Dimensions of a load's bounding box.
/// </summary>
[PublicAPI]
public record LoadDimensionsDto
{
    /// <summary>Absolute length of the load's bounding box in meters.</summary>
    public double Length { get; init; }

    /// <summary>Absolute width of the load's bounding box in meters.</summary>
    public double Width { get; init; }

    /// <summary>Absolute height of the load's bounding box in meters.</summary>
    public double? Height { get; init; }
}

/// <summary>
/// State of an action that is currently being executed or waiting for execution.
/// </summary>
[PublicAPI]
public record ActionStateDto
{
    /// <summary>
    /// Unique actionId.
    /// </summary>
    public string ActionId { get; init; } = string.Empty;

    /// <summary>
    /// actionType of the action.
    /// </summary>
    public string? ActionType { get; init; }

    /// <summary>
    /// Additional information on the current action.
    /// </summary>
    public string? ActionDescription { get; init; }

    /// <summary>
    /// Current status of the action.
    /// </summary>
    public ActionStatusDto ActionStatus { get; init; }

    /// <summary>
    /// Description of the result, e.g., the result of a RFID-read.
    /// </summary>
    public string? ResultDescription { get; init; }
}

/// <summary>
/// State of an action.
/// </summary>
[PublicAPI]
public enum ActionStatusDto
{
    /// <summary>Waiting for the trigger (passing the node, entering the edge).</summary>
    Waiting,

    /// <summary>Action is being initialized.</summary>
    Initializing,

    /// <summary>Action is running.</summary>
    Running,

    /// <summary>Paused by instantAction or external trigger.</summary>
    Paused,

    /// <summary>Action completed successfully.</summary>
    Finished,

    /// <summary>Action could not be performed.</summary>
    Failed
}

/// <summary>
/// Contains all battery-related information.
/// </summary>
[PublicAPI]
public record BatteryStateDto
{
    /// <summary>
    /// State of charge in %.
    /// </summary>
    public double BatteryCharge { get; init; }

    /// <summary>
    /// Battery voltage.
    /// </summary>
    public double? BatteryVoltage { get; init; }

    /// <summary>
    /// State of health in percent.
    /// </summary>
    public double? BatteryHealth { get; init; }

    /// <summary>
    /// True: charging in progress. False: AGV is currently not charging.
    /// </summary>
    public bool Charging { get; init; }

    /// <summary>
    /// Estimated reach with current state of charge in meters.
    /// </summary>
    public double? Reach { get; init; }
}

/// <summary>
/// Error reported by the AGV.
/// </summary>
[PublicAPI]
public record ErrorDto
{
    /// <summary>
    /// Type/name of error.
    /// </summary>
    public string ErrorType { get; init; } = string.Empty;

    /// <summary>
    /// Array of references to provide more information related to the error.
    /// </summary>
    public IList<ErrorReferenceDto>? ErrorReferences { get; init; }

    /// <summary>
    /// Verbose description providing details and possible causes of the error.
    /// </summary>
    public string? ErrorDescription { get; init; }

    /// <summary>
    /// Hint on how to approach or solve the reported error.
    /// </summary>
    public string? ErrorHint { get; init; }

    /// <summary>
    /// Severity level of the error.
    /// </summary>
    public ErrorLevelDto ErrorLevel { get; init; }
}

/// <summary>
/// Reference to provide more information related to an error.
/// </summary>
[PublicAPI]
public record ErrorReferenceDto
{
    /// <summary>
    /// Specifies the type of reference used (e.g. nodeId, edgeId, orderId, actionId, etc.).
    /// </summary>
    public string ReferenceKey { get; init; } = string.Empty;

    /// <summary>
    /// The value that belongs to the reference key.
    /// </summary>
    public string ReferenceValue { get; init; } = string.Empty;
}

/// <summary>
/// Severity level of an error.
/// </summary>
[PublicAPI]
public enum ErrorLevelDto
{
    /// <summary>AGV is ready to start (e.g., maintenance cycle expiration warning).</summary>
    Warning,

    /// <summary>AGV is not in running condition, user intervention required.</summary>
    Fatal
}

/// <summary>
/// Information reported by the AGV for visualization or debugging purposes.
/// </summary>
[PublicAPI]
public record InformationDto
{
    /// <summary>
    /// Type/name of information.
    /// </summary>
    public string InfoType { get; init; } = string.Empty;

    /// <summary>
    /// Array of references related to the information.
    /// </summary>
    public IList<InfoReferenceDto>? InfoReferences { get; init; }

    /// <summary>
    /// Info description.
    /// </summary>
    public string? InfoDescription { get; init; }

    /// <summary>
    /// Level of the information.
    /// </summary>
    public InfoLevelDto InfoLevel { get; init; }
}

/// <summary>
/// Reference related to an information object.
/// </summary>
[PublicAPI]
public record InfoReferenceDto
{
    /// <summary>
    /// References the type of reference (e.g., headerId, orderId, actionId, etc.).
    /// </summary>
    public string ReferenceKey { get; init; } = string.Empty;

    /// <summary>
    /// References the value which belongs to the reference key.
    /// </summary>
    public string ReferenceValue { get; init; } = string.Empty;
}

/// <summary>
/// Level of an information object.
/// </summary>
[PublicAPI]
public enum InfoLevelDto
{
    /// <summary>Used for visualization.</summary>
    Info,

    /// <summary>Used for debugging.</summary>
    Debug
}

/// <summary>
/// Contains all safety-related information.
/// </summary>
[PublicAPI]
public record SafetyStateDto
{
    /// <summary>
    /// Acknowledge type of the e-stop.
    /// </summary>
    public EStopDto EStop { get; init; }

    /// <summary>
    /// Protective field violation. True: field is violated. False: field is not violated.
    /// </summary>
    public bool FieldViolation { get; init; }
}

/// <summary>
/// Acknowledge type of an e-stop.
/// </summary>
[PublicAPI]
public enum EStopDto
{
    /// <summary>Auto-acknowledgeable e-stop is activated, e.g., by bumper or protective field.</summary>
    AutoAck,

    /// <summary>E-stop has to be acknowledged manually at the vehicle.</summary>
    Manual,

    /// <summary>Facility e-stop has to be acknowledged remotely.</summary>
    Remote,

    /// <summary>No e-stop activated.</summary>
    None
}

/// <summary>
/// Current position of the AGV on a map in world coordinates.
/// </summary>
[PublicAPI]
public record Vda5050AgvPositionDto
{
    /// <summary>X coordinate in meters.</summary>
    public double X { get; init; }

    /// <summary>Y coordinate in meters.</summary>
    public double Y { get; init; }

    /// <summary>Orientation of the AGV in radians.</summary>
    public double Theta { get; init; }

    /// <summary>Unique identifier of the map the position refers to.</summary>
    public string MapId { get; init; } = string.Empty;

    /// <summary>Additional information on the map.</summary>
    public string? MapDescription { get; init; }

    /// <summary>
    /// True: position is initialized. False: position is not initialized.
    /// </summary>
    public bool PositionInitialized { get; init; }

    /// <summary>
    /// Describes the quality of the localization. 0.0: position unknown, 1.0: position known.
    /// </summary>
    public double? LocalizationScore { get; init; }

    /// <summary>
    /// Value for position deviation range in meters.
    /// </summary>
    public double? DeviationRange { get; init; }
}

/// <summary>
/// The AGV's velocity in vehicle coordinates.
/// </summary>
[PublicAPI]
public record VelocityDto
{
    /// <summary>The AGV's velocity in its x direction.</summary>
    public double? Vx { get; init; }

    /// <summary>The AGV's velocity in its y direction.</summary>
    public double? Vy { get; init; }

    /// <summary>The AGV's turning speed around its z axis.</summary>
    public double? Omega { get; init; }
}