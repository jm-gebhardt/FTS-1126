namespace DeviceCommunication.Vda5050.Simulator.Domain;

public enum ConnectionState
{
    Online,
    Offline,
    ConnectionBroken
}

public enum ActionStatus
{
    Waiting,
    Initializing,
    Running,
    Paused,
    Finished,
    Failed
}

public enum ErrorLevel
{
    Warning,
    Fatal
}

public sealed record ActionState(string ActionId, string ActionType, ActionStatus Status);

public sealed record NodeState(string NodeId, int SequenceId, bool Released);

public sealed record EdgeState(string EdgeId, int SequenceId, bool Released);

public sealed record ErrorReference(string Key, string Value);

public sealed record Error(string ErrorType, ErrorLevel Level, string? Description, IReadOnlyList<ErrorReference>? ErrorReferences);

public sealed record AgvPosition(double X, double Y, double Theta, string MapId, bool PositionInitialized);

public sealed record Load(string LoadId, string? LoadType, string? LoadPosition);

public sealed record Velocity(double Vx, double Vy, double Omega);

/// <summary>
/// Immutable view of the AGV state at one point in (virtual) time, ready to be published.
/// </summary>
public sealed record AgvStateSnapshot(
    long HeaderId,
    DateTimeOffset Timestamp,
    string OrderId,
    int OrderUpdateId,
    string LastNodeId,
    int LastNodeSequenceId,
    bool Driving,
    double BatteryCharge,
    bool Charging,
    IReadOnlyList<ActionState> ActionStates,
    IReadOnlyList<NodeState> NodeStates,
    IReadOnlyList<EdgeState> EdgeStates,
    IReadOnlyList<Error> Errors,
    AgvPosition? Position,
    Gebhardt.Acc.Common.Domain.Models.AgvOperatingMode OperatingMode,
    IReadOnlyList<Load> Loads,
    Velocity Velocity);