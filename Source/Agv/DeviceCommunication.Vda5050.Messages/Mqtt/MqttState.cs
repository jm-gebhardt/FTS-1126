using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;

/// <summary>
/// All encompassing state of the AGV.
/// <para>See "state.schema".</para>
/// </summary>
[PublicAPI]
[EntityName("amq.topic")] // Specifies the RabbitMq exchange where the message is published
[RoutingKey("*.*.*.*.state")] // Specifies the topic where subscribers can receive the message
public record MqttState
{
    /// <summary>
    /// HeaderId of the message.
    /// The headerId is defined per topic and incremented by 1 with each sent (but not necessarily received) message.
    /// </summary>
    public int HeaderId { get; set; }

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
    /// Array of map-objects that are currently stored on the vehicle.
    /// </summary>
    public ICollection<MqttMap>? Maps { get; set; }

    /// <summary>
    /// Unique order identification of the current order or the previous finished order. The
    /// orderId is kept until a new order is received. Empty string (â€œâ€) if no previous
    /// orderId is available.
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Order Update Identification to identify that an order
    /// update has been accepted by the AGV. \"0\" if no previous orderUpdateId is available.
    /// </summary>
    public int OrderUpdateId { get; set; }

    /// <summary>
    /// Unique ID of the zone set that the AGV currently uses for path planning.
    /// Optional: If the AGV does not use zones, this field can be omitted.
    /// </summary>
    public string? ZoneSetId { get; set; }

    /// <summary>
    /// NodeID of last reached node or, if AGV is currently on a node,
    /// current node (e.g., \"node7\"). Empty string (\"\") if no lastNodeId is available.
    /// </summary>
    public string? LastNodeId { get; set; }

    /// <summary>
    /// SequenceId of the last reached node or, if the AGV is currently on a node, sequenceId of
    /// current node.
    /// â€œ0â€ if no lastNodeSequenceId is available.
    /// </summary>
    public int LastNodeSequenceId { get; set; }

    /// <summary>
    /// True: indicates that the AGV is driving and/or rotating. Other movements of the AGV (e.g.
    /// lift movements) are not included here.
    /// False: indicates that the AGV is neither driving nor rotating
    /// </summary>
    public bool Driving { get; set; }

    /// <summary>
    /// True: AGV is currently in a paused state, either because of the push of a physical button
    /// on the AGV or because of an instantAction. The AGV can resume the order.
    /// False: The AGV is currently not in a paused state.
    /// </summary>
    public bool? Paused { get; set; }

    /// <summary>
    /// True: AGV is almost at the end of the base and will reduce speed if no new base is transmitted.
    /// Trigger for master control to send new base.
    /// False: no base update required.
    /// </summary>
    public bool? NewBaseRequest { get; set; }

    /// <summary>
    /// Used by line guided vehicles to indicate the distance it has been driving past the lastNodeId.
    /// Distance is in meters.
    /// </summary>
    public float? DistanceSinceLastNode { get; set; }

    /// <summary>
    /// Current operating mode of the AGV.
    /// </summary>
    public MqttOperatingMode OperatingMode { get; set; }

    /// <summary>
    /// Array of nodeState-objects that need to be traversed for fulfilling the order. Empty list if idle.
    /// </summary>
    public IList<MqttNodeState> NodeStates { get; set; } = [];

    /// <summary>
    /// Array of edgeState-objects that need to be traversed for fulfilling the order. Empty list if idle.
    /// </summary>
    public IList<MqttEdgeState> EdgeStates { get; set; } = [];

    /// <summary>
    /// Current position of the AGV on the map.
    /// </summary>
    public MqttAgvPosition? AgvPosition { get; set; }

    /// <summary>
    /// The AGV's velocity in vehicle coordinates.
    /// </summary>
    public MqttVelocity? Velocity { get; set; }

    /// <summary>
    /// Loads that are currently handled by the AGV.
    /// Optional: If AGV cannot determine load state, leave null.
    /// If the AGV can determine the load state, but the array is empty, the AGV is considered unloaded.
    /// </summary>
    public IList<MqttLoad>? Loads { get; set; }

    /// <summary>
    /// Contains a list of the current actions and the actions which are yet to be finished.
    /// </summary>
    public IList<MqttActionState> ActionStates { get; set; } = [];

    /// <summary>
    /// Contains all battery-related information.
    /// </summary>
    public MqttBatteryState BatteryState { get; set; } = new();

    /// <summary>
    /// Array of error-objects. All active errors of the AGV should be in the list.
    /// An empty array indicates that the AGV has no active errors.
    /// </summary>
    public IList<MqttError> Errors { get; set; } = [];

    /// <summary>
    /// Array of info-objects. An empty array indicates that the AGV has no information.
    /// This should only be used for visualization or debugging.
    /// </summary>
    public IList<MqttInformation>? Information { get; set; }

    /// <summary>
    /// Contains all safety-related information.
    /// </summary>
    public MqttSafetyState SafetyState { get; set; } = new();
}

/// <summary>
/// Current operating mode of the AGV.
/// </summary>
[PublicAPI]
public enum MqttOperatingMode
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
public record MqttMap
{
    /// <summary>
    /// ID of the map describing a defined area of the vehicle's workspace.
    /// </summary>
    public string MapId { get; set; } = string.Empty;

    /// <summary>
    /// Version of the map.
    /// </summary>
    public string MapVersion { get; set; } = string.Empty;

    /// <summary>
    /// Additional information on the map.
    /// </summary>
    public string? MapDescription { get; set; }

    /// <summary>
    /// Information on the status of the map indicating, if a map version is currently used on the vehicle.
    /// </summary>
    public MqttMapStatus MapStatus { get; set; }
}

/// <summary>
/// Information on the status of the map.
/// </summary>
[PublicAPI]
public enum MqttMapStatus
{
    /// <summary>
    /// Indicates this map is currently active / used on the AGV.
    /// At most one map with the same mapId can have its status set to ENABLED.
    /// </summary>
    Enabled,

    /// <summary>
    /// Indicates this map version is currently not enabled on the AGV and thus could be enabled or deleted by request.
    /// </summary>
    Disabled
}

/// <summary>
/// State of a node that needs to be traversed for fulfilling the order.
/// </summary>
[PublicAPI]
public record MqttNodeState
{
    /// <summary>
    /// Unique node identification.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// SequenceId to discern multiple nodes with same nodeId.
    /// </summary>
    public int SequenceId { get; set; }

    /// <summary>
    /// Additional information on the node.
    /// </summary>
    public string? NodeDescription { get; set; }

    /// <summary>
    /// True: indicates that the node is part of the base.
    /// False: indicates that the node is part of the horizon.
    /// </summary>
    public bool Released { get; set; }

    /// <summary>
    /// Node position. Optional: master control has this information.
    /// Can be sent additionally, e.g., for debugging purposes.
    /// </summary>
    public MqttNodePosition? NodePosition { get; set; }
}

/// <summary>
/// Position of a node on the map.
/// </summary>
[PublicAPI]
public record MqttNodePosition
{
    /// <summary>X coordinate in meters.</summary>
    public double X { get; set; }

    /// <summary>Y coordinate in meters.</summary>
    public double Y { get; set; }

    /// <summary>Orientation of the AGV in radians.</summary>
    public double? Theta { get; set; }

    /// <summary>Unique identifier of the map the position refers to.</summary>
    public string MapId { get; set; } = string.Empty;
}

/// <summary>
/// State of an edge that needs to be traversed for fulfilling the order.
/// </summary>
[PublicAPI]
public record MqttEdgeState
{
    /// <summary>
    /// Unique edge identification.
    /// </summary>
    public string EdgeId { get; set; } = string.Empty;

    /// <summary>
    /// SequenceId of the edge.
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
    /// The trajectory to be communicated as a NURBS.
    /// </summary>
    public MqttTrajectory? Trajectory { get; set; }
}

/// <summary>
/// Load that is currently handled by the AGV.
/// </summary>
[PublicAPI]
public record MqttLoad
{
    /// <summary>
    /// Unique identification number of the load (e.g., barcode or RFID).
    /// </summary>
    public string? LoadId { get; set; }

    /// <summary>Type of load.</summary>
    public string? LoadType { get; set; }

    /// <summary>
    /// Indicates which load handling/carrying unit of the AGV is used.
    /// Optional for vehicles with only one loadPosition.
    /// </summary>
    public string? LoadPosition { get; set; }

    /// <summary>
    /// Point of reference for the location of the bounding box.
    /// </summary>
    public MqttBoundingBoxReference? BoundingBoxReference { get; set; }

    /// <summary>
    /// Dimensions of the load's bounding box in meters.
    /// </summary>
    public MqttLoadDimensions? LoadDimensions { get; set; }

    /// <summary>
    /// Absolute weight of the load measured in kg.
    /// </summary>
    public double? Weight { get; set; }
}

/// <summary>
/// State of an action that is currently being executed or waiting for execution.
/// </summary>
[PublicAPI]
public record MqttActionState
{
    /// <summary>
    /// Unique actionId.
    /// </summary>
    public string ActionId { get; set; } = string.Empty;

    /// <summary>
    /// actionType of the action.
    /// Optional: only for informational or visualization purposes.
    /// </summary>
    public string? ActionType { get; set; }

    /// <summary>
    /// Additional information on the current action.
    /// </summary>
    public string? ActionDescription { get; set; }

    /// <summary>
    /// Current status of the action.
    /// </summary>
    public MqttActionStatus ActionStatus { get; set; }

    /// <summary>
    /// Description of the result, e.g., the result of a RFID-read.
    /// Errors will be transmitted in errors.
    /// </summary>
    public string? ResultDescription { get; set; }
}

/// <summary>
/// Status of an action.
/// </summary>
[PublicAPI]
public enum MqttActionStatus
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
public record MqttBatteryState
{
    /// <summary>
    /// State of charge in %.
    /// </summary>
    public double BatteryCharge { get; set; }

    /// <summary>
    /// Battery voltage.
    /// </summary>
    public double? BatteryVoltage { get; set; }

    /// <summary>
    /// State of health in percent.
    /// </summary>
    public double? BatteryHealth { get; set; }

    /// <summary>
    /// True: charging in progress. False: AGV is currently not charging.
    /// </summary>
    public bool Charging { get; set; }

    /// <summary>
    /// Estimated reach with current state of charge in meters.
    /// </summary>
    public double? Reach { get; set; }
}

/// <summary>
/// Error reported by the AGV.
/// </summary>
[PublicAPI]
public record MqttError
{
    /// <summary>
    /// Type/name of error.
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Array of references to provide more information related to the error.
    /// </summary>
    public IList<MqttErrorReference>? ErrorReferences { get; set; }

    /// <summary>
    /// Verbose description providing details and possible causes of the error.
    /// </summary>
    public string? ErrorDescription { get; set; }

    /// <summary>
    /// Hint on how to approach or solve the reported error.
    /// </summary>
    public string? ErrorHint { get; set; }

    /// <summary>
    /// Severity level of the error.
    /// </summary>
    public MqttErrorLevel ErrorLevel { get; set; }
}

/// <summary>
/// Reference to provide more information related to an error.
/// </summary>
[PublicAPI]
public record MqttErrorReference
{
    /// <summary>
    /// Specifies the type of reference used (e.g. nodeId, edgeId, orderId, actionId, etc.).
    /// </summary>
    public string ReferenceKey { get; set; } = string.Empty;

    /// <summary>
    /// The value that belongs to the reference key.
    /// </summary>
    public string ReferenceValue { get; set; } = string.Empty;
}

/// <summary>
/// Severity level of an error.
/// </summary>
[PublicAPI]
public enum MqttErrorLevel
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
public record MqttInformation
{
    /// <summary>
    /// Type/name of information.
    /// </summary>
    public string InfoType { get; set; } = string.Empty;

    /// <summary>
    /// Array of references related to the information.
    /// </summary>
    public IList<MqttInfoReference>? InfoReferences { get; set; }

    /// <summary>
    /// Info description.
    /// </summary>
    public string? InfoDescription { get; set; }

    /// <summary>
    /// Level of the information.
    /// </summary>
    public MqttInfoLevel InfoLevel { get; set; }
}

/// <summary>
/// Reference related to an information object.
/// </summary>
[PublicAPI]
public record MqttInfoReference
{
    /// <summary>
    /// References the type of reference (e.g., headerId, orderId, actionId, etc.).
    /// </summary>
    public string ReferenceKey { get; set; } = string.Empty;

    /// <summary>
    /// References the value which belongs to the reference key.
    /// </summary>
    public string ReferenceValue { get; set; } = string.Empty;
}

/// <summary>
/// Level of an information object.
/// </summary>
[PublicAPI]
public enum MqttInfoLevel
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
public record MqttSafetyState
{
    /// <summary>
    /// Acknowledge type of the e-stop.
    /// </summary>
    public MqttEStop EStop { get; set; }

    /// <summary>
    /// Protective field violation.
    /// True: field is violated. False: field is not violated.
    /// </summary>
    public bool FieldViolation { get; set; }
}

/// <summary>
/// Acknowledge type of an e-stop.
/// </summary>
[PublicAPI]
public enum MqttEStop
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