namespace Gebhardt.Acc.Vda5050Simulator.Messaging.Dtos;

public sealed record ConnectionMessageDto
{
    public long HeaderId { get; init; }
    public string Timestamp { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public string ConnectionState { get; init; } = string.Empty;
}

public sealed record ActionStateDto(string ActionId, string ActionType, string ActionStatus);

public sealed record NodeStateDto(string NodeId, int SequenceId, bool Released);

public sealed record EdgeStateDto(string EdgeId, int SequenceId, bool Released);

public sealed record ErrorReferenceDto(string ReferenceKey, string ReferenceValue);

public sealed record ErrorDto(string ErrorType, string ErrorLevel, string ErrorDescription, List<ErrorReferenceDto> ErrorReferences);

public sealed record BatteryStateDto(double BatteryCharge, bool Charging);

public sealed record SafetyStateDto(string EStop, bool FieldViolation);

public sealed record AgvPositionDto(double X, double Y, double Theta, string MapId, bool PositionInitialized);

public sealed record StateMessageDto
{
    public long HeaderId { get; init; }
    public string Timestamp { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public string OrderId { get; init; } = string.Empty;
    public int OrderUpdateId { get; init; }
    public string LastNodeId { get; init; } = string.Empty;
    public int LastNodeSequenceId { get; init; }
    public bool Driving { get; init; }
    public AgvPositionDto? AgvPosition { get; init; }
    public List<ActionStateDto> ActionStates { get; init; } = [];
    public BatteryStateDto BatteryState { get; init; } = new(0, false);
    public string OperatingMode { get; init; } = "AUTOMATIC";
    public List<ErrorDto> Errors { get; init; } = [];
    public List<NodeStateDto> NodeStates { get; init; } = [];
    public List<EdgeStateDto> EdgeStates { get; init; } = [];
    public SafetyStateDto SafetyState { get; init; } = new("NONE", false);
}
