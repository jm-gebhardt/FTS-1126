using Gebhardt.StoreWare.Agv.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages
{
    /// <summary>
    /// all encompassing state of the AGV.
    /// </summary>
    [PublicAPI]
    [EntityName("amq.topic")]
    [RoutingKey("agv.status")]
    public record MqttState
    {
        /// <summary>
        /// Serial number of the AGV.
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// Unique order identification of the current order or the previous finished order. The
        /// orderId is kept until a new order is received. Empty string (â€œâ€) if no previous
        /// orderId is available.
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// Contains a list of the current actions and the actions which are yet to be finished. This
        /// may include actions from previous nodes that are still in progress
        /// When an action is completed, an updated state message is published with actionStatus set
        /// to finished and if applicable with the corresponding resultDescription. The actionStates
        /// are kept until a new order is received.
        /// </summary>
        public List<ActionState>? ActionStates { get; set; }

        /// <summary>
        /// The AGVs position
        /// </summary>
        public AgvPosition? AgvPosition { get; set; }

        /// <summary>
        /// All information relating to the battery.
        /// </summary>
        public BatteryState? BatteryState { get; set; }

        /// <summary>
        /// Used by line guided vehicles to indicate the distance it has been driving past the
        /// last NodeId.
        /// Distance is in meters
        /// </summary>
        public float? DistanceSinceLastNode { get; set; }

        /// <summary>
        /// True: indicates that the AGV is driving and/or rotating. Other movements of the AGV (e.g.
        /// lift movements) are not included here.
        /// False: indicates that the AGV is neither driving nor rotating
        /// </summary>
        public bool Driving { get; set; }

        /// <summary>
        /// Information about the edges the AGV still has to drive over. Empty list if the AGV is
        /// idle.
        /// </summary>
        public List<EdgeState>? EdgeStates { get; set; }

        /// <summary>
        /// Array of errors. Errors are kept until resolution.
        /// </summary>
        public List<Error>? Errors { get; set; }

        /// <summary>
        /// headerId of the message. The headerId is defined per topic and incremented by 1 with each
        /// sent (but not necessarily received) message.
        /// </summary>
        public int HeaderId { get; set; }

        /// <summary>
        /// Array of information messages. Messages are only for visualization/debugging. There's no
        /// specification when these messages are deleted.
        /// </summary>
        public List<Information>? Information { get; set; }

        /// <summary>
        /// nodeID of last reached node or, if AGV is currently on a node, current node.
        /// Empty string if no lastNodeId is available.
        /// </summary>
        public string? LastNodeId { get; set; }

        /// <summary>
        /// sequenceId of the last reached node or, if the AGV is currently on a node, sequenceId of
        /// current node.
        /// â€œ0â€ if no lastNodeSequenceId is available.
        /// </summary>
        public int LastNodeSequenceId { get; set; }

        /// <summary>
        /// Array for information about the loads that an AGV currently carries, if the AGV has any
        /// information about them. This array is optional: if an AGV cannot reason about its load
        /// state, it shall not send this field. If an empty field is sent, MC is to assume that the
        /// AGV can reason about its load state and that the AGV currently does not carry a load.
        /// </summary>
        public List<Load>? Loads { get; set; }

        /// <summary>
        /// Manufacturer of the AGV
        /// </summary>
        public string? Manufacturer { get; set; }

        /// <summary>
        /// True: AGV is almost at the end of the base and will reduce speed if no new base is
        /// transmitted. Trigger for MC to send ne base
        /// False: no base update required
        /// </summary>
        public bool? NewBaseRequest { get; set; }

        /// <summary>
        /// Information about the nodes the AGV still has to drive over. Empty list if idle.
        /// </summary>
        public List<NodeState>? NodeStates { get; set; }

        /// <summary>
        /// Current operating mode of the AGV.
        /// </summary>
        public OperatingMode OperatingMode { get; set; }

        /// <summary>
        /// Order Update Identification to identify that an order update has been accepted by the
        /// AGV. â€œ0â€ if no previous orderUpdateId is available.
        /// </summary>
        public int OrderUpdateId { get; set; }

        /// <summary>
        /// True: AGV is currently in a paused state, either because of the push of a physical button
        /// on the AGV or because of an instantAction. The AGV can resume the order.
        /// False: The AGV is currently not in a paused state.
        /// </summary>
        public bool? Paused { get; set; }

        /// <summary>
        /// Object that holds information about the safety status
        /// </summary>
        public SafetyState? SafetyState { get; set; }

        /// <summary>
        /// Timestamp in ISO8601 format.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// The AGVs velocity in vehicle coordinates
        /// </summary>
        public Velocity? Velocity { get; set; }

        /// <summary>
        /// Version of the protocol [Major].[Minor].[Patch]
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Unique ID of the zone set that the AGV currently uses for path planning. Must be the same
        /// as the one used in the order, otherwise the AGV is to reject the order.
        /// Optional: If the AGV does not use zones, this field can be omitted.
        /// </summary>
        public string? ZoneSetId { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="MqttState"/>
        /// </summary>
        public MqttState(string serialNumber, string orderId)
        {
            SerialNumber = serialNumber;
            OrderId = orderId;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [PublicAPI]
    public record ActionState
    {
        /// <summary>
        /// Additional information on the action.
        /// </summary>
        public string? ActionDescription { get; set; }

        /// <summary>
        /// Unique actionId
        /// </summary>
        public string? ActionId { get; set; }

        /// <summary>
        /// WAITING: waiting for trigger
        /// FAILED: action could not be performed.
        /// </summary>
        public ActionStatus ActionStatus { get; set; }

        /// <summary>
        /// actionType of the action.
        /// Optional: Only for informational or visualization purposes. Order knows the type.
        /// </summary>
        public string? ActionType { get; set; }

        /// <summary>
        /// Description of the result, e.g. the result of a RFID read.
        /// </summary>
        public string? ResultDescription { get; set; }
    }

    /// <summary>
    /// The AGVs position
    /// </summary>
    [PublicAPI]
    public record AgvPosition
    {
        /// <summary>
        /// Value for position deviation range in meters. Can be used if the AGV is able to derive it.
        /// </summary>
        public float? DeviationRange { get; set; }

        /// <summary>
        /// Describes the quality of the localization and therefore, can be used e. g. by SLAM-AGVs
        /// to describe how accurate the current position information is.
        /// 0.0: position unknown
        /// 1.0: position known
        /// Optional for vehicles that cannot estimate their localization score.
        /// Only for logging and visualization purposes
        /// </summary>
        public float? LocalizationScore { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? MapDescription { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? MapId { get; set; }

        /// <summary>
        /// True if the AGVs position is initialized, false, if position is not initialized.
        /// </summary>
        public bool PositionInitialized { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public float Theta { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float X { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float Y { get; set; }
    }

    /// <summary>
    /// All information relating to the battery.
    /// </summary>
    [PublicAPI]
    public record BatteryState
    {
        /// <summary>
        /// AgvState of Charge:
        /// If AGV only provides values for good or bad battery levels, these will be indicated as
        /// 20% (bad) and 80% (good).
        /// </summary>
        public float BatteryCharge { get; set; }

        /// <summary>
        /// AgvState of health in percent.
        /// </summary>
        public float? BatteryHealth { get; set; }

        /// <summary>
        /// Battery voltage
        /// </summary>
        public float? BatteryVoltage { get; set; }

        /// <summary>
        /// If true: Charging in progress.
        /// </summary>
        public bool Charging { get; set; }

        /// <summary>
        /// estimated reach with actual AgvState of Charge
        /// </summary>
        public float? Reach { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [PublicAPI]
    public record EdgeState
    {
        /// <summary>
        /// Verbose Edge description
        /// </summary>
        public string? EdgeDescription { get; set; }

        /// <summary>
        /// Unique edge identification
        /// </summary>
        public string? EdgeId { get; set; }

        /// <summary>
        /// True: Edge is part of base. False: Edge is part of horizon.
        /// </summary>
        public bool Released { get; set; }

        /// <summary>
        /// sequenceId of the edge.
        /// </summary>
        public int SequenceId { get; set; }

        /// <summary>
        /// The trajectory is to be communicated as a NURBS and is defined in chapter 5.4 Topic:
        /// Order (from master control to AGV).
        /// Trajectory segments are from the point where the AGV starts to enter the edge until the
        /// point where it reports that the next node was traversed.
        /// </summary>
        public Trajectory? Trajectory { get; set; }
    }

    /// <summary>
    /// The trajectory is to be communicated as a NURBS and is defined in chapter 5.4 Topic:
    /// Order (from master control to AGV).
    /// Trajectory segments are from the point where the AGV starts to enter the edge until the
    /// point where it reports that the next node was traversed.
    /// </summary>
    [PublicAPI]
    public record Trajectory
    {
        /// <summary>
        /// List of JSON controlPoint objects defining the control points of the NURBS. This includes
        /// the start and end point.
        /// </summary>
        public List<ControlPoint>? ControlPoints { get; set; }

        /// <summary>
        /// The degree of the NURBS.
        /// </summary>
        public int Degree { get; set; }

        /// <summary>
        /// Sequence of parameter values that determine where and how the control points affect the
        /// NURBS curve. knotVector has size of number of control points + degree + 1
        /// </summary>
        public List<float>? KnotVector { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [PublicAPI]
    public record ControlPoint
    {
        /// <summary>
        /// The weight with which this control point pulls no the curve.
        /// When not defined, the default will be 1.0.
        /// </summary>
        public float Weight { get; set; }

        /// <summary>
        /// The x coordinate of the control point
        /// </summary>
        public float X { get; set; }
        /// <summary>
        /// The y coordinate of the control point
        /// </summary>
        public float Y { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [PublicAPI]
    public record Error
    {
        /// <summary>
        /// Verbose description of error.
        /// </summary>
        public string? ErrorDescription { get; set; }

        /// <summary>
        /// warning: AGV is ready to drive without human intervention
        /// fatal: AGV is not in running condition.
        /// </summary>
        public ErrorLevel ErrorLevel { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<ErrorReference>? ErrorReferences { get; set; }

        /// <summary>
        /// Type of error.
        /// </summary>
        public string? ErrorType { get; set; }
    }

    /// <summary>
    /// Object that holds the error reference (e.g. orderId, orderUpdateId, actionId...) as
    /// key-value pairs.
    /// </summary>
    [PublicAPI]
    public record ErrorReference
    {
        /// <summary>
        /// 
        /// </summary>
        public string? ReferenceKey { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string? ReferenceValue { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [PublicAPI]
    public record Information
    {
        /// <summary>
        /// Verbose description of error.
        /// </summary>
        public string? InfoDescription { get; set; }

        /// <summary>
        /// Type of information
        /// </summary>
        public InfoLevel InfoLevel { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<InfoReference>? InfoReferences { get; set; }

        /// <summary>
        /// Type of Information.
        /// </summary>
        public string? InfoType { get; set; }
    }

    /// <summary>
    /// Object that holds the info reference (e.g. orderId, orderUpdateId, actionId...) as
    /// key-value pairs.
    /// </summary>
    [PublicAPI]
    public record InfoReference
    {
        /// <summary>
        /// 
        /// </summary>
        public string? ReferenceKey { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string? ReferenceValue { get; set; }
    }

    /// <summary>
    /// Load object that describes the load if the AGV has information about it.
    /// </summary>
    [PublicAPI]
    public record Load
    {
        /// <summary>
        /// This point describes the loads position on the AGV in the vehicle coordinates. The
        /// boundingBoxReference point is in the middle of the footprint of the load, so length/2 and
        /// width/2.
        /// </summary>
        public BoundingBoxReference? BoundingBoxReference { get; set; }

        /// <summary>
        /// Dimensions of the loads bounding box in meters.
        /// </summary>
        public LoadDimensions? LoadDimensions { get; set; }

        /// <summary>
        /// Unique identification number of the load (e. g. barcode or RFID)
        /// Empty field if the AGV can identify the load but didn't identify the load yet.
        /// Optional if the AGV has cannot identify the load.
        /// </summary>
        public string? LoadId { get; set; }

        /// <summary>
        /// Indicates which load handling/carrying unit of the AGV is used, e. g. in case the AGV has
        /// multiple spots/positions to carry loads.
        /// For example: front, back, positionC1, etc.
        /// Optional for vehicles with only one loadPosition.
        /// </summary>
        public string? LoadPosition { get; set; }

        /// <summary>
        /// The type of the load
        /// </summary>
        public string? LoadType { get; set; }

        /// <summary>
        /// Weight of load in kg
        /// </summary>
        public float? Weight { get; set; }
    }

    /// <summary>
    /// This point describes the loads position on the AGV in the vehicle coordinates. The
    /// boundingBoxReference point is in the middle of the footprint of the load, so length/2 and
    /// width/2.
    /// </summary>
    [PublicAPI]
    public record BoundingBoxReference
    {
        /// <summary>
        /// 
        /// </summary>
        public float? Theta { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float X { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float Y { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float Z { get; set; }
    }

    /// <summary>
    /// Dimensions of the load's bounding box in meters.
    /// </summary>
    [PublicAPI]
    public record LoadDimensions
    {
        /// <summary>
        /// Absolute height of the loads bounding box in meter.
        /// Optional:
        /// Set value only if known.
        /// </summary>
        public float? Height { get; set; }

        /// <summary>
        /// Absolute length of the loads bounding box in meter.
        /// </summary>
        public float Length { get; set; }

        /// <summary>
        /// Absolute width of the loads bounding box in meter.
        /// </summary>
        public float Width { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [PublicAPI]
    public record NodeState
    {
        /// <summary>
        /// Verbose node description
        /// </summary>
        public string? NodeDescription { get; set; }

        /// <summary>
        /// Unique node identification
        /// </summary>
        public string? NodeId { get; set; }

        /// <summary>
        /// Node position. The object is defined in chapter 5.4 Topic: Order (from master control to
        /// AGV).
        /// Optional:
        /// Master control has this information. Can be sent additionally, e. g. for debugging
        /// purposes.
        /// </summary>
        public NodePosition? NodePosition { get; set; }

        /// <summary>
        /// True: indicates that the node is part of the base. False: indicates that the node is part
        /// of the horizon.
        /// </summary>
        public bool Released { get; set; }

        /// <summary>
        /// sequenceId of the node.
        /// </summary>
        public int SequenceId { get; set; }
    }

    /// <summary>
    /// Node position. The object is defined in chapter 5.4 Topic: Order (from master control to
    /// AGV).
    /// Optional:
    /// Master control has this information. Can be sent additionally, e. g. for debugging
    /// purposes.
    /// </summary>
    [PublicAPI]
    public record NodePosition
    {
        /// <summary>
        /// The id of the node in the map
        /// </summary>
        public string? MapId { get; set; }
        /// <summary>
        /// Orientation of the node in the map
        /// </summary>
        public float Theta { get; set; }
        /// <summary>
        /// X coordinate of the node in the map
        /// </summary>
        public float X { get; set; }
        /// <summary>
        /// Y coordinate of the node in the map
        /// </summary>
        public float Y { get; set; }
    }

    /// <summary>
    /// Object that holds information about the safety status
    /// </summary>
    [PublicAPI]
    public record SafetyState
    {
        /// <summary>
        /// autoAck, manual, remote, none
        /// </summary>
        public EStop EStop { get; set; }

        /// <summary>
        /// Protective field violation
        /// </summary>
        public bool FieldViolation { get; set; }
    }

    /// <summary>
    /// The AGVs velocity in vehicle coordinates
    /// </summary>
    [PublicAPI]
    public record Velocity
    {
        /// <summary>
        /// The direction of the velocity vector
        /// </summary>
        public float? Omega { get; set; }
        /// <summary>
        /// The x value of the velocity vector
        /// </summary>
        public float? Vx { get; set; }
        /// <summary>
        /// The y value of the velocity vector
        /// </summary>
        public float? Vy { get; set; }
    }

    /// <summary>
    /// WAITING: waiting for trigger
    /// FAILED: action could not be performed.
    /// </summary>
    [PublicAPI]
    public enum ActionStatus
    {
        /// <summary>
        /// 
        /// </summary>
        Failed,
        /// <summary>
        /// 
        /// </summary>
        Finished,
        /// <summary>
        /// 
        /// </summary>
        Initializing,
        /// <summary>
        /// 
        /// </summary>
        Running,
        /// <summary>
        /// 
        /// </summary>
        Waiting
    };

    /// <summary>
    /// warning: AGV is ready to drive without human intervention
    /// fatal: AGV is not in running condition.
    /// </summary>
    [PublicAPI]
    public enum ErrorLevel
    {
        /// <summary>
        /// 
        /// </summary>
        Fatal,
        /// <summary>
        /// 
        /// </summary>
        Warning
    };

    /// <summary>
    /// Type of information
    /// </summary>
    [PublicAPI]
    public enum InfoLevel
    {
        /// <summary>
        /// 
        /// </summary>
        Debug,
        /// <summary>
        /// 
        /// </summary>
        Info
    };

    /// <summary>
    /// Current operating mode of the AGV.
    /// </summary>
    [PublicAPI]
    public enum OperatingMode
    {
        /// <summary>
        /// 
        /// </summary>
        Automatic,
        /// <summary>
        /// 
        /// </summary>
        Manual,
        /// <summary>
        /// 
        /// </summary>
        Semiautomatic,
        /// <summary>
        /// 
        /// </summary>
        Service,
        /// <summary>
        /// 
        /// </summary>
        Teachin
    };

    /// <summary>
    /// autoAck, manual, remote, none
    /// </summary>
    [PublicAPI]
    public enum EStop
    {
        /// <summary>
        /// 
        /// </summary>
        AutoAck,
        /// <summary>
        /// 
        /// </summary>
        Manual,
        /// <summary>
        /// 
        /// </summary>
        None,
        /// <summary>
        /// 
        /// </summary>
        Remote
    };
}
