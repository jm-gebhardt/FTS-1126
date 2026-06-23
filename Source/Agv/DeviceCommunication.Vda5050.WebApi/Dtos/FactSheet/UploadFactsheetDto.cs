using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.FactSheet;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Dtos.FactSheet;

/// <summary>
/// Request body for uploading an AGV factsheet.
/// </summary>
[PublicAPI]
public record UploadFactsheetDto
{
    /// <summary>Header ID of the message.</summary>
    public int HeaderId { get; set; }

    /// <summary>Timestamp in ISO8601 format.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Version of the VD[M]A-5050 protocol [Major].[Minor].[Patch].</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Manufacturer of the AGV.</summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>Serial number of the AGV.</summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>General classification and capabilities of the AGV.</summary>
    public TypeSpecificationDto TypeSpecification { get; set; } = new();

    /// <summary>Basic physical properties of the AGV.</summary>
    public PhysicalParametersDto PhysicalParameters { get; set; } = new();

    /// <summary>Protocol limitations of the AGV.</summary>
    public ProtocolLimitsDto ProtocolLimits { get; set; } = new();

    /// <summary>Supported features of the VDA5050 protocol.</summary>
    public ProtocolFeaturesDto ProtocolFeatures { get; set; } = new();

    /// <summary>Detailed definition of AGV geometry.</summary>
    public AgvGeometryDto AgvGeometry { get; set; } = new();

    /// <summary>Abstract specification of load capabilities.</summary>
    public LoadSpecificationDto LoadSpecification { get; set; } = new();

    /// <summary>Hardware and software version and network configuration of the vehicle. Optional.</summary>
    public VehicleConfigDto? VehicleConfig { get; set; }
}

/// <summary>General classification and capabilities of the AGV type series.</summary>
[PublicAPI]
public record TypeSpecificationDto
{
    /// <summary>Free text generalized series name as specified by manufacturer.</summary>
    public string SeriesName { get; set; } = string.Empty;

    /// <summary>Free text human-readable description of the AGV type series.</summary>
    public string? SeriesDescription { get; set; }

    /// <summary>Simplified description of AGV kinematics type.</summary>
    public AgvKinematic AgvKinematic { get; set; }

    /// <summary>Simplified description of AGV class.</summary>
    public AgvClass AgvClass { get; set; }

    /// <summary>Maximum loadable mass in kg.</summary>
    public double MaxLoadMass { get; set; }

    /// <summary>Simplified description of localization types supported by the AGV.</summary>
    public ICollection<LocalizationType> LocalizationTypes { get; set; } = [];

    /// <summary>List of path planning types supported by the AGV, sorted by priority.</summary>
    public ICollection<NavigationType> NavigationTypes { get; set; } = [];
}

/// <summary>Basic physical properties of the AGV.</summary>
[PublicAPI]
public record PhysicalParametersDto
{
    /// <summary>Minimal controlled continuous speed of the AGV in m/s.</summary>
    public double SpeedMin { get; set; }

    /// <summary>Maximum speed of the AGV in m/s.</summary>
    public double SpeedMax { get; set; }

    /// <summary>Maximum acceleration with maximum load in m/s².</summary>
    public double AccelerationMax { get; set; }

    /// <summary>Maximum deceleration with maximum load in m/s².</summary>
    public double DecelerationMax { get; set; }

    /// <summary>Minimum height of the AGV in meters. Optional.</summary>
    public double? HeightMin { get; set; }

    /// <summary>Maximum height of the AGV in meters.</summary>
    public double HeightMax { get; set; }

    /// <summary>Width of the AGV in meters.</summary>
    public double Width { get; set; }

    /// <summary>Length of the AGV in meters.</summary>
    public double Length { get; set; }
}

/// <summary>Protocol limitations of the AGV.</summary>
[PublicAPI]
public record ProtocolLimitsDto
{
    /// <summary>Maximum lengths of strings.</summary>
    public MaxStringLensDto MaxStringLens { get; set; } = new();

    /// <summary>Maximum lengths of arrays.</summary>
    public MaxArrayLensDto MaxArrayLens { get; set; } = new();

    /// <summary>Timing information.</summary>
    public TimingDto Timing { get; set; } = new();
}

/// <summary>Maximum lengths of string fields in the protocol.</summary>
[PublicAPI]
public record MaxStringLensDto
{
    /// <summary>Maximum MQTT message length.</summary>
    public int? MsgLen { get; set; }

    /// <summary>Maximum length of the serial-number part in MQTT topics.</summary>
    public int? TopicSerialLen { get; set; }

    /// <summary>Maximum length of all other parts in MQTT topics.</summary>
    public int? TopicElemLen { get; set; }

    /// <summary>Maximum length of ID strings.</summary>
    public int? IdLen { get; set; }

    /// <summary>If true, ID strings must contain numerical values only.</summary>
    public bool? IdNumericalOnly { get; set; }

    /// <summary>Maximum length of enum and key strings.</summary>
    public int? EnumLen { get; set; }

    /// <summary>Maximum length of loadId strings.</summary>
    public int? LoadIdLen { get; set; }
}

/// <summary>Maximum number of elements in array fields in the protocol.</summary>
[PublicAPI]
public record MaxArrayLensDto
{
    /// <summary>Maximum number of nodes per order processable by the AGV.</summary>
    [JsonPropertyName("order.nodes")]
    public int? OrderNodes { get; set; }

    /// <summary>Maximum number of edges per order processable by the AGV.</summary>
    [JsonPropertyName("order.edges")]
    public int? OrderEdges { get; set; }

    /// <summary>Maximum number of actions per node processable by the AGV.</summary>
    [JsonPropertyName("node.actions")]
    public int? NodeActions { get; set; }

    /// <summary>Maximum number of actions per edge processable by the AGV.</summary>
    [JsonPropertyName("edge.actions")]
    public int? EdgeActions { get; set; }

    /// <summary>Maximum number of parameters per action processable by the AGV.</summary>
    [JsonPropertyName("actions.actionsParameters")]
    public int? ActionsParameters { get; set; }

    /// <summary>Maximum number of instant actions per message processable by the AGV.</summary>
    [JsonPropertyName("instantActions")]
    public int? InstantActions { get; set; }

    /// <summary>Maximum number of knots per trajectory processable by the AGV.</summary>
    [JsonPropertyName("trajectory.knotVector")]
    public int? TrajectoryKnotVector { get; set; }

    /// <summary>Maximum number of control points per trajectory processable by the AGV.</summary>
    [JsonPropertyName("trajectory.controlPoints")]
    public int? TrajectoryControlPoints { get; set; }

    /// <summary>Maximum number of nodeStates sent by the AGV / maximum nodes in base.</summary>
    [JsonPropertyName("state.nodeStates")]
    public int? StateNodeStates { get; set; }

    /// <summary>Maximum number of edgeStates sent by the AGV / maximum edges in base.</summary>
    [JsonPropertyName("state.edgeStates")]
    public int? StateEdgeStates { get; set; }

    /// <summary>Maximum number of load objects sent by the AGV.</summary>
    [JsonPropertyName("state.loads")]
    public int? StateLoads { get; set; }

    /// <summary>Maximum number of actionStates sent by the AGV.</summary>
    [JsonPropertyName("state.actionStates")]
    public int? StateActionStates { get; set; }

    /// <summary>Maximum number of errors sent by the AGV in one state message.</summary>
    [JsonPropertyName("state.errors")]
    public int? StateErrors { get; set; }

    /// <summary>Maximum number of information objects sent by the AGV in one state message.</summary>
    [JsonPropertyName("state.information")]
    public int? StateInformation { get; set; }

    /// <summary>Maximum number of error references sent by the AGV for each error.</summary>
    [JsonPropertyName("error.errorReferences")]
    public int? ErrorReferences { get; set; }

    /// <summary>Maximum number of info references sent by the AGV for each information object.</summary>
    [JsonPropertyName("information.infoReferences")]
    public int? InformationInfoReferences { get; set; }
}

/// <summary>Timing constraints for AGV protocol communication.</summary>
[PublicAPI]
public record TimingDto
{
    /// <summary>Minimum interval for sending order messages to the AGV in seconds.</summary>
    public double MinOrderInterval { get; set; }

    /// <summary>Minimum interval for sending state messages in seconds.</summary>
    public double MinStateInterval { get; set; }

    /// <summary>Default interval for sending state messages in seconds. Optional.</summary>
    public double? DefaultStateInterval { get; set; }

    /// <summary>Default interval for sending messages on the visualization topic in seconds. Optional.</summary>
    public double? VisualizationInterval { get; set; }
}

/// <summary>Supported features of the VDA5050 protocol.</summary>
[PublicAPI]
public record ProtocolFeaturesDto
{
    /// <summary>List of supported and/or required optional parameters.</summary>
    public ICollection<OptionalParameterDto> OptionalParameters { get; set; } = [];

    /// <summary>List of all actions with parameters supported by this AGV.</summary>
    public ICollection<FactsheetActionDto> AgvActions { get; set; } = [];
}

/// <summary>An optional protocol parameter and its support level.</summary>
[PublicAPI]
public record OptionalParameterDto
{
    /// <summary>Full name of the optional parameter, e.g. "order.nodes.nodePosition.allowedDeviationTheta".</summary>
    public string Parameter { get; set; } = string.Empty;

    /// <summary>Type of support for the optional parameter.</summary>
    public ParameterSupport Support { get; set; }

    /// <summary>Free text description of the optional parameter.</summary>
    public string? Description { get; set; }
}

/// <summary>An action type declared in the factsheet, including its parameters and applicable scopes.</summary>
[PublicAPI]
public record FactsheetActionDto
{
    /// <summary>Unique actionType corresponding to action.actionType.</summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>Free text description of the action.</summary>
    public string? ActionDescription { get; set; }

    /// <summary>List of allowed scopes for using this action type.</summary>
    public ICollection<ActionScope> ActionScopes { get; set; } = [];

    /// <summary>List of parameters for this action. Omit or leave empty if the action has no parameters.</summary>
    public ICollection<FactsheetActionParameterDto>? ActionParameters { get; set; }

    /// <summary>Free text description of the result.</summary>
    public string? ResultDescription { get; set; }

    /// <summary>Array of possible blocking types for this action.</summary>
    public ICollection<BlockingType>? BlockingTypes { get; set; }
}

/// <summary>Definition of a parameter accepted by a factsheet action.</summary>
[PublicAPI]
public record FactsheetActionParameterDto
{
    /// <summary>Key string for the parameter.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Data type of the parameter value.</summary>
    public ValueDataType ValueDataType { get; set; }

    /// <summary>Free text description of the parameter.</summary>
    public string? Description { get; set; }

    /// <summary>True: this parameter is optional.</summary>
    public bool? IsOptional { get; set; }
}

/// <summary>Detailed geometry of the AGV.</summary>
[PublicAPI]
public record AgvGeometryDto
{
    /// <summary>List of wheels, containing wheel arrangement and geometry.</summary>
    public ICollection<WheelDefinitionDto>? WheelDefinitions { get; set; }

    /// <summary>List of AGV envelope curves in 2D.</summary>
    // ReSharper disable once InconsistentNaming
    public ICollection<Envelope2dDto>? Envelopes2d { get; set; }

    /// <summary>List of AGV envelope curves in 3D.</summary>
    // ReSharper disable once InconsistentNaming
    public ICollection<Envelope3dDto>? Envelopes3d { get; set; }
}

/// <summary>Geometry and arrangement of a single wheel.</summary>
[PublicAPI]
public record WheelDefinitionDto
{
    /// <summary>Wheel type.</summary>
    public WheelType Type { get; set; }

    /// <summary>True: wheel is actively driven.</summary>
    public bool IsActiveDriven { get; set; }

    /// <summary>True: wheel is actively steered.</summary>
    public bool IsActiveSteered { get; set; }

    /// <summary>Position of the wheel in the AGV coordinate system.</summary>
    public WheelPositionDto Position { get; set; } = new();

    /// <summary>Nominal diameter of the wheel in meters.</summary>
    public double Diameter { get; set; }

    /// <summary>Nominal width of the wheel in meters.</summary>
    public double Width { get; set; }

    /// <summary>Nominal displacement of the wheel's center to the rotation point in meters. Assumed 0 if not defined.</summary>
    public double? CenterDisplacement { get; set; }

    /// <summary>Free text constraints defined by the manufacturer.</summary>
    public string? Constraints { get; set; }
}

/// <summary>Position of a wheel in the AGV coordinate system.</summary>
[PublicAPI]
public record WheelPositionDto
{
    /// <summary>X-position in meters.</summary>
    public double X { get; set; }

    /// <summary>Y-position in meters.</summary>
    public double Y { get; set; }

    /// <summary>Orientation in radians. Necessary for fixed wheels.</summary>
    public double? Theta { get; set; }
}

/// <summary>AGV envelope curve in 2D (polygon).</summary>
[PublicAPI]
// ReSharper disable once InconsistentNaming
public record Envelope2dDto
{
    /// <summary>Name of the envelope curve set.</summary>
    public string Set { get; set; } = string.Empty;

    /// <summary>Envelope curve as a closed, non-self-intersecting x/y polygon.</summary>
    public ICollection<PolygonPointDto> PolygonPoints { get; set; } = [];

    /// <summary>Free text description of the envelope curve set.</summary>
    public string? Description { get; set; }
}

/// <summary>A single point in a 2D polygon.</summary>
[PublicAPI]
public record PolygonPointDto
{
    /// <summary>X-position of the polygon point in meters.</summary>
    public double X { get; set; }

    /// <summary>Y-position of the polygon point in meters.</summary>
    public double Y { get; set; }
}

/// <summary>AGV envelope curve in 3D.</summary>
[PublicAPI]
// ReSharper disable once InconsistentNaming
public record Envelope3dDto
{
    /// <summary>Name of the envelope curve set.</summary>
    public string Set { get; set; } = string.Empty;

    /// <summary>Format of the data, e.g. "DXF".</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>3D envelope curve data in the format specified by <see cref="Format"/>.</summary>
    public object? Data { get; set; }

    /// <summary>URL for downloading the 3D envelope curve data.</summary>
    public string? Url { get; set; }

    /// <summary>Free text description of the envelope curve set.</summary>
    public string? Description { get; set; }
}

/// <summary>Abstract specification of load capabilities of the AGV.</summary>
[PublicAPI]
public record LoadSpecificationDto
{
    /// <summary>List of load positions / load handling devices. If empty or absent, the AGV has no load handling device.</summary>
    public ICollection<string>? LoadPositions { get; set; }

    /// <summary>List of load sets that can be handled by the AGV.</summary>
    public ICollection<LoadSetDto>? LoadSets { get; set; }
}

/// <summary>A set of loads that the AGV can handle, with associated handling constraints.</summary>
[PublicAPI]
public record LoadSetDto
{
    /// <summary>Unique name of the load set, e.g. "DEFAULT", "SET1".</summary>
    public string SetName { get; set; } = string.Empty;

    /// <summary>Type of load, e.g. "EPAL", "XLT1200".</summary>
    public string LoadType { get; set; } = string.Empty;

    /// <summary>List of load positions this load set is valid for. If empty, valid for all load handling devices.</summary>
    public ICollection<string>? LoadPositions { get; set; }

    /// <summary>Bounding box reference as defined in state.loads[].</summary>
    public BoundingBoxReferenceDto? BoundingBoxReference { get; set; }

    /// <summary>Dimensions of the load's bounding box.</summary>
    public LoadDimensionsDto? LoadDimensions { get; set; }

    /// <summary>Maximum weight of this load type in kg.</summary>
    public double? MaxWeight { get; set; }

    /// <summary>Minimum allowed height for handling this load type in meters.</summary>
    public double? MinLoadhandlingHeight { get; set; }

    /// <summary>Maximum allowed height for handling this load type in meters.</summary>
    public double? MaxLoadhandlingHeight { get; set; }

    /// <summary>Minimum allowed depth for this load type in meters.</summary>
    public double? MinLoadhandlingDepth { get; set; }

    /// <summary>Maximum allowed depth for this load type in meters.</summary>
    public double? MaxLoadhandlingDepth { get; set; }

    /// <summary>Minimum allowed tilt for this load type in radians.</summary>
    public double? MinLoadhandlingTilt { get; set; }

    /// <summary>Maximum allowed tilt for this load type in radians.</summary>
    public double? MaxLoadhandlingTilt { get; set; }

    /// <summary>Maximum allowed speed for this load type in m/s.</summary>
    public double? AgvSpeedLimit { get; set; }

    /// <summary>Maximum allowed acceleration for this load type in m/s².</summary>
    public double? AgvAccelerationLimit { get; set; }

    /// <summary>Maximum allowed deceleration for this load type in m/s².</summary>
    public double? AgvDecelerationLimit { get; set; }

    /// <summary>Approximate time for picking up the load in seconds.</summary>
    public double? PickTime { get; set; }

    /// <summary>Approximate time for dropping the load in seconds.</summary>
    public double? DropTime { get; set; }

    /// <summary>Free text description of the load handling set.</summary>
    public string? Description { get; set; }
}

/// <summary>Bounding box reference point for a load set.</summary>
[PublicAPI]
public record BoundingBoxReferenceDto
{
    /// <summary>X-coordinate of the reference point.</summary>
    public double X { get; set; }

    /// <summary>Y-coordinate of the reference point.</summary>
    public double Y { get; set; }

    /// <summary>Z-coordinate of the reference point.</summary>
    public double Z { get; set; }

    /// <summary>Orientation of the load's bounding box. Optional.</summary>
    public double? Theta { get; set; }
}

/// <summary>Dimensions of a load's bounding box.</summary>
[PublicAPI]
public record LoadDimensionsDto
{
    /// <summary>Absolute length of the load's bounding box.</summary>
    public double Length { get; set; }

    /// <summary>Absolute width of the load's bounding box.</summary>
    public double Width { get; set; }

    /// <summary>Absolute height of the load's bounding box. Optional.</summary>
    public double? Height { get; set; }
}

/// <summary>Hardware/software version and network configuration of the vehicle.</summary>
[PublicAPI]
public record VehicleConfigDto
{
    /// <summary>Array of hardware and software versions running on the vehicle.</summary>
    public ICollection<VehicleVersionDto>? Versions { get; set; }

    /// <summary>Network configuration of the vehicle.</summary>
    public NetworkConfigDto? Network { get; set; }
}

/// <summary>A hardware or software version entry.</summary>
[PublicAPI]
public record VehicleVersionDto
{
    /// <summary>The key identifying the version component, e.g. "softwareVersion", "cameraVersion".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>The version value, e.g. "v1.03.2", "0x4297F30C".</summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>Network configuration of the vehicle.</summary>
[PublicAPI]
public record NetworkConfigDto
{
    /// <summary>List of DNS servers used by the vehicle.</summary>
    public ICollection<string>? DnsServers { get; set; }

    /// <summary>A priori assigned IP address of the vehicle used to communicate with the MQTT broker.</summary>
    public string? LocalIpAddress { get; set; }

    /// <summary>List of NTP servers used by the vehicle.</summary>
    public ICollection<string>? NtpServers { get; set; }

    /// <summary>Network subnet mask.</summary>
    public string? Netmask { get; set; }

    /// <summary>Default gateway used by the vehicle.</summary>
    public string? DefaultGateway { get; set; }
}
